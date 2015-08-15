﻿using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;


namespace Efekt
{
    public sealed class Interpreter : IAsiVisitor<IAsi>
    {
        Env env;
        Asi current;
        Env global;
        ValidationList validations;


        public IAsi Run(AsiList al, ValidationList validationList)
        {
            validations = validationList;
            global = env = new Env();
            var res = VisitAsiList(al);
            global = null;
            return res;
        }


        public IAsi VisitAsiList(AsiList al) => visitAsiArray(al.Items, new Env(env), env);


        public IAsi VisitErr(Err err) => err;


        public IAsi VisitInt(Int ii) => ii;


        public IAsi VisitIdent(Ident i)
        {
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
                case "=":
                    var v = opa.Op2.Accept(this);
                    v = copyIfStructInstance(v);
                    var ma = opa.Op1 as BinOpApply;
                    if (ma != null && ma.Op.Name == ".")
                    {
                        var s = ma.Op1.Accept(this);
                        var e = getStructEnvOfMember(s, ma.Op2);
                        e.SetValue(((Ident) ma.Op2).Name, v);
                    }
                    else
                    {
                        var i = declare(opa.Op1);
                        env.SetValue(i.Name, v);
                    }
                    return v;
                case ".":
                    return getStructMember(opa.Op1.Accept(this), opa.Op2);
                default:
                    return VisitFnApply(new FnApply(opa.Op, new List<IExp> {opa.Op1, opa.Op2}));
            }
        }


        IAsi copyIfStructInstance(IAsi asi)
        {
            var s = asi as Struct;
            if (s?.Env == null)
                return asi;
            var newEnv = new Env(global);
            newEnv.CopyFrom(s.Env);
            foreach (var kvp in newEnv.Dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
                newEnv.SetValue(kvp.Key, copyIfStructInstance(kvp.Value));
            return new Struct(new List<IAsi>()) {Env = newEnv};
        }


        IAsi getStructMember(IAsi bag, IAsi member)
            => getStructEnvOfMember(bag, member).GetValue(((Ident) member).Name);


        Env getStructEnvOfMember(IAsi bag, IAsi member)
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


        Ident declare(IAsi declrOrIdent)
        {
            var d = declrOrIdent as Declr;
            if (d == null)
            {
                var i = declrOrIdent as Ident;
                if (i == null)
                    validations.DeclrExpected(declrOrIdent);
                return i;
            }
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
            Contract.Assume(!arr.IsEvaluated);
            return new Arr(arr.Items
                .Select(i => i.Accept(this))
                .Cast<IExp>()
                .ToList())
            {IsEvaluated = true};
        }


        public IAsi VisitStruct(Struct s)
        {
            current = s;
            return s;
        }


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
            var bRes = applyBuiltin(fna);
            if (bRes != null)
                return bRes;

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
            var envForParams = new Env(fn.Env);
            evalParamsAndArgs(fn, fna.Fn, fna.Args.ToArray(), envForParams);
            return visitAsiArray(fn.Items, envForParams, prevEnv);
        }


        IAsi applyBuiltin(FnApply fna)
        {
            var fnIdent = fna.Fn as Ident;
            if (fnIdent == null || !fnIdent.Name.StartsWith("__"))
                return null;
            return Builtins.Call(fnIdent.Name.Substring(2), evalArgs(fna.Args));
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
            env = new Env(global);
            var instance = new Struct(new List<IAsi>()) {Env = env};
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
                    var i = declare(opa.Op1);
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
            Contract.Assume(instance.Env.Parent == global);
            return instance;
        }


        public IAsi VisitVoid(Void v) => v;

        public IAsi VisitBool(Bool b) => b;


        IAsi visitAsiArray(IReadOnlyList<IAsi> items, Env newEnv, Env restoreEnv)
        {
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