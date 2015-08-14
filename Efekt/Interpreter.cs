using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Interpreter : IAsiVisitor<IAsi>
    {
        private Env env;
        private Asi current;
        private Env rootEnv;


        public IAsi VisitAsiList(AsiList al)
        {
            Env e;
            if (rootEnv == null)
            {
                e = new Env();
                rootEnv = e;
            }
            else
            {
                e = new Env(env);
            }
            return visitAsiArray(al.Items, e, env);
        }


        public IAsi VisitErr(Err err) => err.Item == null ? err : err.Item.Accept(this);


        public IAsi VisitInt(Int ii) => ii;


        public IAsi VisitIdent(Ident i) => env.GetValue(i.Name);


        public IAsi VisitBinOpApply(BinOpApply opa)
        {
            Contract.Assume(opa != null);
            switch (opa.Op.Name)
            {
                case "=":
                    var evaluated = opa.Op2.Accept(this);
                    evaluated = copyIfStructInstance(evaluated);
                    var ma = opa.Op1 as BinOpApply;
                    if (ma != null && ma.Op.Name == ".")
                    {
                        var e = getStructEnvOfMember(ma.Op1.Accept(this), ma.Op2);
                        e.SetValue(((Ident) ma.Op2).Name, evaluated);
                    }
                    else
                    {
                        var i = getIdentAndDeclareIfDeclr(opa.Op1);
                        env.SetValue(i.Name, evaluated);
                    }
                    return evaluated;
                case ".":
                    return getStructMember(opa.Op1.Accept(this), opa.Op2);
                default:
                    var fna = new FnApply(opa.Op, new[] {opa.Op1, opa.Op2});
                    return VisitFnApply(fna);
            }
        }


        private IAsi copyIfStructInstance([CanBeNull] IAsi asi)
        {
            var s = asi as Struct;
            if (s?.Env == null)
                return asi;
            var newEnv = new Env(rootEnv);
            newEnv.CopyFrom(s.Env);
            foreach (var kvp in newEnv.Dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
                newEnv.SetValue(kvp.Key, copyIfStructInstance(kvp.Value));
            return new Struct(Array.Empty<Asi>()) {Env = newEnv};
        }


        private IAsi getStructMember(IAsi bag, IAsi member)
            => getStructEnvOfMember(bag, member).GetValue(((Ident) member).Name);


        private Env getStructEnvOfMember(IAsi bag, IAsi member)
        {
            var s2 = bag as Struct;
            if (s2 == null)
                throw new EfektException(
                    "cannot access member '" + member.Accept(Program.DefaultPrinter) + "' of " +
                    bag.GetType().Name);
            if (s2.Env == null)
                throw new EfektException(
                    "cannot access member '" + member.Accept(Program.DefaultPrinter) +
                    "'of not constructed struct");

            var m = member as Ident;
            if (m == null)
                throw new EfektException(
                    "expected identifier or member access after '.', not "
                    + member.GetType().Name);
            var sAsi = bag.Accept(this);
            var s = sAsi as Struct;
            if (s == null)
                throw new EfektException(
                    "exp before '." + member.Accept(Program.DefaultPrinter)
                    + "'must evaluate to struct, not " + sAsi.GetType().Name);
            return s.Env;
        }


        private Ident getIdentAndDeclareIfDeclr(IAsi declrOrIdent)
        {
            var d = declrOrIdent as Declr;
            if (d == null)
                return (Ident) declrOrIdent;
            d.Accept(this);
            return d.Ident;
        }


        public IAsi VisitDeclr(Declr d)
        {
            env.Declare(d.Ident.Name);
            return null;
        }


        public IAsi VisitArr(Arr arr)
        {
            Contract.Assume(arr.IsEvaluated == false);
            return new Arr(arr.Items
                .Select(i => i.Accept(this))
                .Cast<IExp>()
                .ToList()) {IsEvaluated = true};
        }


        public IAsi VisitStruct(Struct s)
        {
            current = s;
            return s;
        }


        public IAsi VisitFn(Fn fn) => new Fn(fn.Params, fn.Items) {Env = fn.Env ?? env.GetFlat()};


        public IAsi VisitFnApply(FnApply fna)
        {
            var fnIdent = fna.Fn as Ident;
            if (fnIdent != null && fnIdent.Name.StartsWith("__"))
            {
                var prevEnv1 = env;
                env = new Env(env); // for params
                var evaluatedArgs = fna.Args.Select(arg => arg.Accept(this)).Cast<IExp>().ToArray();
                var r = Builtins.Call(fnIdent.Name.Substring(2), evaluatedArgs);
                env = prevEnv1;
                return r;
            }

            var fnAsi = fna.Fn.Accept(this);
            var fn = fnAsi as Fn;
            if (fnAsi is Struct)
                return new FnApply(fnAsi, fna.Args);
            if (fn == null)
                throw new EfektException("cannot apply " + fnAsi.GetType().Name);

            Contract.Assume(fn.Env != null);
            current = fn;

            var prevEnv = env;
            evalParamsAndArgs(fn.Params.ToArray(), fna.Args.ToArray());
            var localEnv = new Env(env); // new local env for this fn call
            localEnv.CopyFrom(env); // make params local variables of fn
            localEnv.Parent = fn.Env; // reference captured env
            localEnv.ImportedEnvs = fn.Env.ImportedEnvs;
            var res = visitAsiArray(fn.Items, localEnv, prevEnv);
            return res;
        }


        private void evalParamsAndArgs(ICollection<IExp> @params, ICollection<IExp> args)
        {
            env = new Env(env); // for params
            var n = 0;
            foreach (var p in @params)
            {
                var opa = p as BinOpApply;
                if (args.Count <= n)
                {
                    p.Accept(this);
                    if (opa == null)
                        throw new EfektException("fn has " + @params.Count + " parameter(s)" +
                                                 ", but calling with " + args.Count);
                }
                else
                {
                    var arg = args.ElementAt(n);
                    var argValue = arg.Accept(this);
                    argValue = copyIfStructInstance(argValue);
                    var i = Parser.GetIdentFromDeclrLikeAsi(p);
                    if (opa != null)
                        env.Declare(i.Name);
                    else
                        p.Accept(this);
                    env.SetValue(i.Name, argValue);
                }
                ++n;
            }
        }


        public IAsi VisitNew(New n)
        {
            var opa2 = n.Exp as BinOpApply; // new has higher priority than any op
            var eExp = opa2 != null ? opa2.Op1.Accept(this) : n.Exp.Accept(this);

            var fna = eExp as FnApply;
            var s = fna == null ? eExp as Struct : fna.Fn as Struct;

            if (s == null)
                throw new EfektException(
                    "expression after new should evaluate to struct or fn apply, not "
                    + eExp.GetType().Name);

            Contract.Assume(s.Env == null);
            var prevEnv = env;
            env = new Env(rootEnv);
            var instance = new Struct(Array.Empty<Asi>()) {Env = env};
            current = instance;
            foreach (var item in s.Items)
            {
                var declrItem = item as Declr;
                var opa = item as BinOpApply;
                if (opa == null)
                {
                    var imp = item as Import;
                    if (imp == null)
                    {
                        if (declrItem == null)
                            throw new EfektException("struct can contains only variables");
                        if (!declrItem.IsVar)
                            throw new EfektException(
                                "declaration must be prefixed with 'var' in struct");
                        declrItem.Accept(this);
                    }
                    else
                    {
                        imp.Accept(this);
                    }
                }
                else if (opa.Op.Name == "=")
                {
                    // provide error as above
                    Contract.Assume(opa.Op1 is Declr);
                    var i = getIdentAndDeclareIfDeclr(opa.Op1);
                    var v = opa.Op2.Accept(this);
                    env.SetValue(i.Name, v);
                }
                else
                {
                    throw new EfektException("struct can contains only variables, found: " + opa.Op);
                }
            }

            if (fna != null)
            {
                var c = (Fn) env.GetValue("constructor");
                c.Env = env;
                var fna2 = new FnApply(c, fna.Args);
                VisitFnApply(fna2);
            }
            if (opa2 != null)
            {
                env = prevEnv;
                return new BinOpApply(opa2.Op, instance, opa2.Op2).Accept(this);
            }
            env = prevEnv;
            Contract.Assume(instance.Env != null);
            Contract.Assume(instance.Env.Parent == rootEnv);
            return instance;
        }


        public IAsi VisitVoid(Void v) => v;

        public IAsi VisitBool(Bool b) => b;


        private IAsi visitAsiArray(IEnumerable<IAsi> items, Env newEnv, Env restoreEnv)
        {
            env = newEnv;
            IAsi res = null;
            var n = 0;
            var itemList = items.ToList();
            foreach (var item in itemList)
            {
                if (++n != itemList.Count && item is Val)
                    Program.ValidationList.AddExpHasNoEffect(item);
                else
                    res = item.Accept(this);
            }
            env = restoreEnv;
            return copyIfStructInstance(res) ?? new Void();
        }


        public IAsi VisitChar(Char c) => c;


        public IAsi VisitIf(If iff)
        {
            var t = iff.Test.Accept(this);
            var b = t as Bool;
            if (b == null)
                throw new EfektException("test in if must evaluated to bool, not to: " +
                                         t.GetType().Name);
            return b.Value
                ? iff.Then.Accept(this)
                : iff.Otherwise == null ? new Void() : iff.Otherwise.Accept(this);
        }


        public IAsi VisitImport(Import imp)
        {
            var asi = imp.QualifiedIdent.Accept(this);
            var s = asi as Struct;
            if (s == null || s.Env == null)
                throw new EfektException(
                    "only constructed struct or fn can be imported, not " + asi.GetType().Name);

            var sTo = current as Struct;
            var fTo = current as Fn;
            if (sTo == null && fTo == null)
                throw new EfektException(
                    "import can be present only in struct or fn, not " + asi.GetType().Name);
            if (sTo != null)
                sTo.Env.AddImport(s.Env);
            else if (fTo != null)
                fTo.Env.AddImport(s.Env);
            return new Void();
        }
    }
}