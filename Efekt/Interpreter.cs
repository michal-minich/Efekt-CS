using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;


namespace Efekt
{
    public sealed class Interpreter : IAsiVisitor<Asi>
    {
        private Env env;


        public Asi VisitAsiList(AsiList al) => visitAsiArray(al.Items, new Env(env), env);


        public Asi VisitInt(Int ii) => ii;


        public Asi VisitIdent(Ident ident) => env.GetValue(ident.Name);


        public Asi VisitBinOpApply(BinOpApply opa)
        {
            Contract.Requires(opa != null);
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


        private static Asi copyIfStructInstance(Asi asi)
        {
            var s = asi as Struct;
            if (s?.Env == null)
                return asi;
            var newEnv = new Env(null);
            newEnv.CopyFrom(s.Env);
            foreach (var kvp in newEnv.Dict.ToDictionary(kvp=>kvp.Key, kvp=>kvp.Value))
                newEnv.SetValue(kvp.Key, copyIfStructInstance(kvp.Value));
            return new Struct(Array.Empty<Asi>()) {Env = newEnv};
        }


        private Asi getStructMember(IAsi bag, IAsi member)
            => getStructEnvOfMember(bag, member).GetValue(((Ident) member).Name);


        private Env getStructEnvOfMember(IAsi bag, IAsi member)
        {
            var s2 = bag as Struct;
            if (s2 == null)
                throw new Exception(
                    "cannot access member '" + member.Accept(Program.DefaultPrinter) + "' of " +
                    bag.GetType().Name);
            if (s2.Env == null)
                throw new Exception(
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


        private Ident getIdentAndDeclareIfDeclr(Asi declrOrIdent)
        {
            var d = declrOrIdent as Declr;
            if (d == null)
                return (Ident) declrOrIdent;
            d.Accept(this);
            return d.Ident;
        }


        public Asi VisitDeclr(Declr d)
        {
            env.Declare(d.Ident.Name);
            return null;
        }


        public Asi VisitArr(Arr arr)
        {
            Contract.Assume(arr.IsEvaluated == false);
            return new Arr(arr.Items.Select(i => i.Accept(this)).ToList()) {IsEvaluated = true};
        }


        public Asi VisitStruct(Struct s) => s;


        public Asi VisitFn(Fn fn)
        {
            Contract.Assume(fn.Env == null);
            return new Fn(fn.Params, fn.Items) {Env = env.GetFlat()};
        }


        public Asi VisitFnApply(FnApply fna)
        {
            var fnIdent = fna.Fn as Ident;
            if (fnIdent != null && fnIdent.Name.StartsWith("__"))
            {
                var prevEnv1 = env;
                env = new Env(env); // for params
                var evaluatedArgs = fna.Args.Select(arg => arg.Accept(this)).ToArray();
                var r = Builtins.Call(fnIdent.Name.Substring(2), evaluatedArgs);
                env = prevEnv1;
                return r;
            }

            var fnAsi = fna.Fn.Accept(this);
            var fn = fnAsi as Fn;
            if (fn == null)
                throw new EfektException("cannot apply " + fnAsi.GetType().Name);
            Contract.Assume(fn.Env != null);

            var prevEnv = env;
            evalParamsAndArgs(fn.Params.ToArray(), fna.Args.ToArray());
            var localEnv = new Env(env); // new local env for this fn call
            localEnv.CopyFrom(env); // make params local variables of fn
            localEnv.Parent = fn.Env; // reference captured env
            var res = visitAsiArray(fn.Items, localEnv, prevEnv);
            return res;
        }


        private void evalParamsAndArgs(ICollection<Asi> @params, ICollection<Asi> args)
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


        public Asi VisitNew(New n)
        {
            var eExp = n.Exp.Accept(this);
            var s = eExp as Struct;
            var fna = eExp as FnApply;

            if (s != null)
            {
                Contract.Assume(s.Env == null);
                var prevEnv = env;
                env = new Env(null);
                foreach (var item in s.Items)
                {
                    var declrItem = item as Declr;
                    var opa = item as BinOpApply;
                    if (opa == null)
                    {
                        if (declrItem == null)
                            throw new Exception("struct can contains only variables");
                        if (!declrItem.IsVar)
                            throw new Exception("declaration must be prefixed with 'var' in struct");
                        declrItem.Accept(this);
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
                        throw new Exception("struct can contains only variables, found: " + opa.Op);
                    }
                }
                var instance = new Struct(Array.Empty<Asi>()) {Env = env};
                env = prevEnv;
                return instance;
            }
            else if (fna != null)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new EfektException(
                    "expression after new should evaluate to struct or fn apply, not "
                    + eExp.GetType().Name);
            }
        }


        public Asi VisitVoid(Void v) => v;

        public Asi VisitBool(Bool b) => b;


        private Asi visitAsiArray(IEnumerable<Asi> items, Env newEnv, Env restoreEnv)
        {
            env = newEnv;
            Asi res = null;
            foreach (var item in items)
                res = item.Accept(this);
            env = restoreEnv;
            return res ?? new Void();
        }
    }
}