using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Interpreter : IAsiVisitor<IAsi>
    {
        private Env env;
        Asi current;
        Env global;
        ValidationList validations;


        public IAsi Run(AsiList al, ValidationList validationList)
        {
            if (al.Items.Count == 0)
                return new Void();
            validations = validationList;
            var fileStruct = new Struct(new List<IAsi>());
            global = env = new Env(fileStruct);
            fileStruct.Env = global;
            visitAsiArray(al.Items.DropLast().ToList(), env, env);
            var last = al.Items.Last();
            IAsi res;
            res = last.Accept(this);
            global = env = null;
            current = null;
            validations = null;
            return res;
        }


        public IAsi VisitAsiList(AsiList al)
            => visitAsiArray(al.Items, new Env(env.Owner, env), env);


        public IAsi VisitErr(Err err) => err;


        public IAsi VisitInt(Int ii) => ii;


        public IAsi VisitIdent(Ident i)
        {
            if (i.Name == "this")
                return env.Owner;
            if (i.Name == "global")
                return global.Owner;
            var v = env.GetValueOrNull(i.Name);
            if (v != null)
                return v;
            validations.ImplicitVar(i);
            return new Err(i);
        }


        public IAsi VisitBinOpApply(BinOpApply opa)
        {
            switch (opa.Op.Name)
            {
                case ".":
                    return getStructEnvOfMember(opa).GetValue(((Ident)opa.Op2).Name);
                default:
                    return VisitFnApply(new FnApply(opa.Op, new List<IExp> { opa.Op1, opa.Op2 }));
            }
        }


        IAsi copyIfStructInstance(IAsi asi)
        {
            var s = asi as Struct;
            if (s?.Env == null || s == global.Owner)
                return asi;
            var newStruct = new Struct(new List<IAsi>());
            var newEnv = new Env(newStruct, global);
            newStruct.Env = newEnv;
            newEnv.CopyFrom(s.Env);
            foreach (var kvp in newEnv.Dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            {
                newEnv.SetValue(kvp.Key, copyIfStructInstance(kvp.Value));
                var fn = kvp.Value as Fn;
                if (fn?.Env != null)
                    fn.Env = newEnv;
            }
            return newStruct;
        }


        Env getStructEnvOfMember(BinOpApply ma)
        {
            var bag = ma.Op1.Accept(this);
            var m = ma.Op2 as Ident;
            if (m == null)
                throw new EfektException(
                    "expected identifier or member access after '.', not "
                    + ma.Op2.GetType().Name);

            var s2 = bag as Struct;
            if (s2 == null)
                throw new EfektException(
                    "cannot access member '" + ma.Op2.Accept(Program.DefaultPrinter) + "' of " +
                    bag.GetType().Name);
            if (s2.Env == null)
                throw new EfektException(
                    "cannot access member '" + ma.Op2.Accept(Program.DefaultPrinter) +
                    "' of not constructed struct");
            return s2.Env;
        }


        Ident declare(IAsi declrOrIdent)
        {
            Contract.Ensures(Contract.Result<Ident>() != null);

            var d = declrOrIdent as Declr;
            if (d == null)
            {
                var i = declrOrIdent as Ident;
                if (i == null)
                {
                    validations.DeclrExpected(declrOrIdent);
                    i = new Ident("__error", IdentCategory.Value);
                    env.Declare(i.Name);
                }
                return i;
            }
            d.Accept(this);
            return d.Ident;
        }


        public IAsi VisitDeclr(Declr d)
        {
            env.Declare(d.Ident.Name);
            return new Void();
        }


        public IAsi VisitArr(Arr arr)
        {
            Contract.Assume(!arr.IsEvaluated);
            return new Arr(arr.Items
                .Select(i => i.Accept(this))
                .Cast<IExp>()
                .ToList())
            { IsEvaluated = true };
        }


        public IAsi VisitStruct(Struct s) => s;


        public IAsi VisitFn(Fn fn)
            => new Fn(fn.Params, fn.Items)
            {
                Env = env,
                CountMandatoryParams = fn.CountMandatoryParams,
                Column = fn.Column,
                Line = fn.Line
            };


        public IAsi VisitFnApply(FnApply fna)
        {
            var fnIdent = fna.Fn as Ident;
            if (fnIdent != null && fnIdent.Name.StartsWith("__"))
                return Builtins.Call(fnIdent.Name.Substring(2), evalArgs(fna.Args));

            var fnAsi = fna.Fn.Accept(this);
            if (fnAsi is Struct)
                return new FnApply(fnAsi, fna.Args);
            var fn = fnAsi as Fn;
            if (fn == null)
            {
                validations.CannotApply(fna.Fn, fnAsi);
                return new Err(fna);
            }

            current = fn;
            var prevEnv = env;
            var envForParams = new Env(fn.Env.Owner, fn.Env);
            evalParamsAndArgs(fn, fna.Fn, fna.Args.ToArray(), envForParams);
            return visitAsiArray(fn.Items, envForParams, prevEnv);
        }


        IExp[] evalArgs(IEnumerable<IExp> args)
            => args.Select(arg => arg.Accept(this)).Cast<IExp>().ToArray();


        void evalParamsAndArgs(Fn fn, IAsi notEvaledFn, IReadOnlyList<IExp> args, Env envForParams)
        {
            IReadOnlyList<IExp> args2;
            if (args.Count < fn.CountMandatoryParams)
            {
                validations.NotEnoughArgs(fn.Params[args.Count], notEvaledFn, fn.Params.Count,
                    fn.CountMandatoryParams, args.Count);
                var missingArgCount = fn.CountMandatoryParams - args.Count;
                var errs = fn.Params.Skip(args.Count).Take(missingArgCount).Select(p => new Err(p));
                args2 = args.Concat(errs).ToList();
            }
            else
            {
                if (args.Count > fn.Params.Count)
                    validations.TooManyArgs(
                        args[fn.Params.Count], notEvaledFn, fn.Params.Count, args.Count);
                args2 = args;
            }
            var n = 0;
            var evaluatedArgs = evalArgs(args2);
            env = envForParams;
            foreach (var p in fn.Params)
            {
                var opa = p as BinOpApply;
                if (args2.Count <= n)
                {
                    p.Accept(this);
                }
                else
                {
                    var argValue = copyIfStructInstance(evaluatedArgs[n]);
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
            var expAsi = n.Exp.Accept(this);
            var fna = expAsi as FnApply;
            var sAsi = fna != null ? fna.Fn : expAsi;
            var s = sAsi as Struct;

            if (s == null)
            {
                validations.NoStructAfterNew(sAsi, sAsi.GetType().Name);
                return new Err(n);
            }
            if (s.Env != null)
            {
                validations.InstanceAfterNew(n.Exp);
                return new Err(n);
            }

            var prevEnv = env;
            var instance = new Struct(new List<IAsi>());
            env = new Env(instance, global);
            instance.Env = env;
            current = instance;
            foreach (var item in s.Items)
                item.Accept(this);
            applyConstructor(n, fna);
            env = prevEnv;
            return instance;
        }


        void applyConstructor(New n, [CanBeNull] FnApply fna)
        {
            var cAsi = env.GetValueOrNull("constructor");
            if (fna != null)
            {
                if (cAsi == null)
                {
                    validations.NoConstructor(n);
                    return;
                }
                var c = cAsi as Fn;
                if (c == null)
                {
                    validations.ConstructorIsNotFn(cAsi);
                    return;
                }
                VisitFnApply(new FnApply(VisitFn(c), fna.Args));
            }
            else if (cAsi != null)
            {
                validations.ConstructorNotCalled(n);
            }
        }


        public IAsi VisitVoid(Void v) => v;

        public IAsi VisitBool(Bool b) => b;


        IAsi visitAsiArray(IReadOnlyList<IAsi> items, Env newEnv, Env restoreEnv)
        {
            Contract.Ensures(Contract.Result<IAsi>() != null);

            if (items.Count == 0)
                return new Void();
            env = newEnv;
            for (var i = 0; i < items.Count - 1; ++i)
            {
                if (items[i] is Val)
                    validations.ExpHasNoEffect(items[i]);
                else
                    items[i].Accept(this);
            }
            var res = items.Last().Accept(this);
            env = restoreEnv;
            return copyIfStructInstance(res);
        }


        public IAsi VisitChar(Char c) => c;


        public IAsi VisitIf(If iff)
        {
            var t = iff.Test.Accept(this);
            var b = t as Bool;
            if (b == null)
                validations.IfTestIsNotBool(t);
            // if 'else' is missing and 'if' is used as stm, then it is and error
            return b != null && b.Value
                ? iff.Then.Accept(this)
                : iff.Otherwise == null ? new Void() : iff.Otherwise.Accept(this);
        }


        public IAsi VisitAssign(Assign a)
        {
            var v = a.Value.Accept(this);
            v = copyIfStructInstance(v);
            var ma = a.Target as BinOpApply;
            if (ma != null && ma.Op.Name == ".")
            {
                getStructEnvOfMember(ma).SetValue(((Ident)ma.Op2).Name, v);
            }
            else
            {
                var i = declare(a.Target);
                env.SetValue(i.Name, v);
            }
            return v;
        }


        public IAsi VisitImport(Import imp)
        {
            var to = current as IHasEnv;
            if (to == null)
            {
                validations.CannotImportTo(imp);
            }
            else
            {
                var asi = imp.QualifiedIdent.Accept(this);
                var s = asi as Struct;
                if (s == null)
                    validations.ImportIsNotStruct(imp.QualifiedIdent);
                else if (s.Env == null)
                    validations.ImportIsStructType(imp.QualifiedIdent);
                else
                    to.Env.AddImport(s.Env);
            }
            return new Void();
        }
    }
}