using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Interpreter : IAsiVisitor<IAsi>
    {
        Env env;
        Asi current;
        Env global;
        ValidationList validations;
        Builtins builtins;
        Boolean isReturn;
        Boolean isBreak;
        Boolean isContinue;


        public IAsi Run(AsiList al, ValidationList validationList)
        {
            if (al.Items.Count == 0)
                return new Void();
            builtins = new Builtins(validationList);
            validations = validationList;
            var fileStruct = new Struct(new List<IAsi>());
            global = env = new Env(validations, fileStruct);
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
            => visitAsiArray(al.Items, new Env(validations, env.Owner, env), env);


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
                    return accessMember(opa);
                default:
                    return VisitFnApply(new FnApply(opa.Op, new List<IExp> { opa.Op1, opa.Op2 }));
            }
        }


        IAsi accessMember(BinOpApply opa)
        {
            var argValue = opa.Op1.Accept(this);
            var member = (Ident)opa.Op2;
            if (argValue is Struct)
            {
                var bag = getStructEnvOfMember(opa, argValue);
                var m = bag.GetOwnValueOrNull(member.Name);
                if (m != null)
                    return m;
            }
            return createMemberFn(member, argValue);
        }


        IAsi createMemberFn(Ident member, IAsi argValue)
        {
            var mExt = env.GetValueOrNull(member.Name);
            if (mExt == null)
                validations.GenericWarning("member is unknown '{0}'", member);
            var mFn = mExt as Fn;
            if (mFn == null)
                validations.GenericWarning("member extension '{0}' must be a func", member);
            var @params = mFn.Params.Skip(1).ToList();
            var e = new Env(validations, mFn.Env.Owner, mFn.Env);
            var i = declare(e, mFn.Params[0]);
            e.SetValue(i.Name, copyIfStructInstance(argValue, mFn.Params[0].Attributes));
            var extFn = new Fn(@params, mFn.BodyItems)
            {
                Env = e,
                CountMandatoryParams = mFn.CountMandatoryParams - 1,
                Line = mFn.Line
            };
            return extFn;
        }


        IAsi copyIfStructInstance(IAsi asi, List<IExp> targetAttrs)
        {
            if (hasSimpleAttr(targetAttrs, "byref"))
                return asi;
            var s = asi as Struct;
            if (s?.Env == null || s == global.Owner)
                return asi;
            var newStruct = new Struct(new List<IAsi>());
            var newEnv = new Env(validations, newStruct, global);
            newStruct.Env = newEnv;
            newEnv.CopyFrom(s.Env);
            foreach (var kvp in newEnv.Dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            {
                newEnv.SetValue(kvp.Key, copyIfStructInstance(kvp.Value.Item, new List<IExp>()));
                var fn = kvp.Value.Item as Fn;
                if (fn?.Env != null)
                    fn.Env = newEnv;
            }
            return newStruct;
        }


        static Boolean hasSimpleAttr(IEnumerable<IExp> attrs, String name)
        {
            var attrName = "@" + name;
            return attrs.OfType<Ident>().Any(aIdent => aIdent.Name == attrName);
        }


        Env getStructEnvOfMember(BinOpApply ma, IAsi bag)
        {
            var m = ma.Op2 as Ident;
            if (m == null)
                validations.GenericWarning(
                    "expected identifier or member access after '.' , not {0} ", ma.Op2);

            var s2 = bag as Struct;
            if (s2 == null)
            {
                validations.GenericWarning(
                    "cannot access member '"
                    + ma.Op2.Accept(Program.DefaultPrinter) + "' of {0}", bag);
                return new Env(validations, env.Owner);
            }
            if (s2.Env == null)
                validations.GenericWarning(
                    "cannot access member '" + ma.Op2.Accept(Program.DefaultPrinter) +
                    "' of not constructed struct", bag);
            return s2.Env;
        }


        Ident declare(Env e, Declr d)
        {
            Contract.Ensures(Contract.Result<Ident>() != null);

            var prevEnv = env;
            env = e;
            d.Accept(this);
            env = prevEnv;
            return d.Ident;
        }


        public IAsi VisitDeclr(Declr d)
        {
            var acc = (env == global)
                ? Accessibility.Global
                : (current is Struct ? Accessibility.Private : Accessibility.Local);

            if (d.Value == null)
            {
                if (hasSimpleAttr(d.Attributes, "public"))
                    env.Declare(Accessibility.Public, d.Ident.Name);
                else
                    env.Declare(acc, d.Ident.Name);
                return new Void();
            }

            var v = d.Value.Accept(this);
            v = copyIfStructInstance(v, new List<IExp>());
            if (hasSimpleAttr(d.Attributes, "public"))
                env.Declare(Accessibility.Public, d.Ident.Name, v);
            else
                env.Declare(acc, d.Ident.Name, v);
            return v;
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
            => new Fn(fn.Params, fn.BodyItems)
            {
                Env = env,
                CountMandatoryParams = fn.CountMandatoryParams,
                Line = fn.Line
            };


        public IAsi VisitFnApply(FnApply fna)
        {
            var fnIdent = fna.Fn as Ident;
            if (fnIdent != null && fnIdent.Name.StartsWith("__"))
                return builtins.Call(fnIdent.Name.Substring(2), evalArgs(fna.Args));

            var fn = fna.Fn as Fn;
            if (fn?.Env == null)
            {
                var fnAsi = fna.Fn.Accept(this);

                if (fnAsi is Struct)
                    return new FnApply(fnAsi, fna.Args);
                fn = fnAsi as Fn;
                if (fn == null)
                {
                    validations.CannotApply(fna.Fn, fnAsi);
                    return new Err(fna);
                }
            }

            current = fn;
            var prevEnv = env;
            var envForParams = new Env(validations, fn.Env.Owner, fn.Env);
            evalParamsAndArgs(fn, fna.Fn, fna.Args.ToArray(), envForParams);
            return visitAsiArray(fn.BodyItems, envForParams, prevEnv);
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
                if (args2.Count <= n)
                {
                    //p.Accept(this);
                    env.Declare(Accessibility.Local, p.Ident.Name, p.Value?.Accept(this));
                }
                else
                {
                    var argValue = copyIfStructInstance(evaluatedArgs[n], p.Attributes);
                    env.Declare(Accessibility.Local, p.Ident.Name, argValue);
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
            env = new Env(validations, instance, global);
            instance.Env = env;
            current = instance;
            foreach (var item in s.Items)
                item.Accept(this);
            var cons = env.GetOwnValueOrNull("constructor");
            env = prevEnv;
            applyConstructor(n, fna, cons);
            return instance;
        }


        void applyConstructor(New n, [CanBeNull] FnApply fna, IAsi cons)
        {
            if (fna != null)
            {
                if (cons == null)
                {
                    validations.NoConstructor(n);
                    return;
                }
                var c = cons as Fn;
                if (c == null)
                {
                    validations.ConstructorIsNotFn(cons);
                    return;
                }
                VisitFnApply(new FnApply(c, fna.Args));
            }
            else if (cons != null)
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
            IAsi r = null;
            foreach (var item in items.DropLast())
            {
                if (item is Val)
                    validations.ExpHasNoEffect(item);
                else
                    r = item.Accept(this);
                if (isReturn)
                {
                    isReturn = false;
                    return r;
                }
            }
            r = items.Last().Accept(this);
            isReturn = false;
            env = restoreEnv;
            return copyIfStructInstance(r, new List<IExp>());
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
            v = copyIfStructInstance(v, new List<IExp>());
            var ma = a.Target as BinOpApply;
            if (ma != null && ma.Op.Name == ".")
            {
                var bag = ma.Op1.Accept(this);
                var e = getStructEnvOfMember(ma, bag);
                var name = ((Ident)ma.Op2).Name;
                env.CheckAccessibility(name, e, "write member");
                e.SetValue(name, v);
                return v;
            }

            var i = a.Target as Ident;
            if (i != null)
            {
                env.SetValue(i.Name, v);
                return v;
            }

            validations.GenericWarning("Canot assign to '{0}'", a.Target);
            env.SetValue("__error", v);
            return v;
        }


        public IAsi VisitGoto(Goto gt)
        {
            throw new NotImplementedException();
        }


        public IAsi VisitLabel(Label lbl)
        {
            throw new NotImplementedException();
        }


        public IAsi VisitBreak(Break br)
        {
            if (br.Test != null)
                isBreak = ((Bool)br.Test.Accept(this)).Value;
            else
                isBreak = true;
            return Void.Instance;
        }


        public IAsi VisitContinue(Continue ct)
        {
            if (ct.Test != null)
                isContinue = ((Bool)ct.Test.Accept(this)).Value;
            else
                isContinue = true;
            return Void.Instance;
        }


        public IAsi VisitReturn(Return r)
        {
            var res = r.Value == null ? Void.Instance : r.Value.Accept(this);
            isReturn = true;
            return res;
        }


        public IAsi VisitRepeat(Repeat rp)
        {
            return repeatWhile(() => true, rp.Items);
        }


        IAsi repeatWhile(Func<Boolean> condition, IReadOnlyCollection<IAsi> items,
            String itemName = null)
        {
            IAsi r = Void.Instance;
            var prevEnv = env;
            env = new Env(validations, env.Owner, env);
            if (itemName != null)
                env.Declare(Accessibility.Local, itemName);
            while (condition())
            {
                cont:
                foreach (var item in items)
                {
                    if (isReturn)
                    {
                        isReturn = false;
                        goto exit;
                    }
                    else if (isBreak)
                    {
                        isBreak = false;
                        goto exit;
                    }
                    else if (isContinue)
                    {
                        isContinue = false;
                        goto cont;
                    }
                    r = item.Accept(this);
                }
            }
            exit:
            env = prevEnv;
            return r;
        }


        public IAsi VisitForEach(ForEach fe)
        {
            var iterable = fe.Iterable.Accept(this);
            var arr = (Arr)iterable;
            var en = arr.Items.GetEnumerator();

            return repeatWhile(() =>
            {
                var hasItem = en.MoveNext();
                env.SetValue(fe.Ident.Name, en.Current);
                return hasItem;
            }, fe.Items, fe.Ident.Name);
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


        public IAsi VisitThrow(Throw th)
        {
            IExp throwed;
            if (th.Ex != null)
                throwed = (IExp)th.Ex.Accept(this);
            else
            {
                throwed = null;
            }
            throw new InterpretedThrowException(throwed);
        }


        public IAsi VisitTry(Try tr)
        {
            var prevEnv = env;
            env = new Env(validations, env.Owner, env);
            try
            {
                visitAsiArray(tr.TryItems, env, prevEnv);
            }
            catch (InterpretedThrowException ex)
            {
                if (tr.CatchItems != null)
                {
                    env = new Env(validations, env.Owner, env);
                    if (tr.ExVar != null)
                        env.Declare(Accessibility.Local, tr.ExVar.Name, ex.Throwed);
                    visitAsiArray(tr.CatchItems, env, prevEnv);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (tr.FinallyItems != null)
                {
                    env = new Env(validations, env.Owner, env);
                    visitAsiArray(tr.FinallyItems, env, prevEnv);
                }
            }
            return Void.Instance;
        }


        static Arr toCharArr(String value)
            => new Arr(value.Select(ch => new Char(ch)).Cast<IExp>().ToList());


        public IAsi VisitAssume(Assume asm)
        {
            var t = (Bool)asm.Exp.Accept(this);
            if (!t.Value)
            {
                var th = new Throw(toCharArr(
                    "Assumption failed: " + asm.Exp.Accept(Program.DefaultPrinter)));
                th.Accept(this);
            }
            return Void.Instance;
        }


        public IAsi VisitAssert(Assert ast)
        {
            var t = (Bool)ast.Exp.Accept(this);
            if (!t.Value)
            {
                var th = new Throw(toCharArr(
                    "Assertion failed: " + ast.Exp.Accept(Program.DefaultPrinter)));
                th.Accept(this);
            }
            return Void.Instance;
        }
    }


    public sealed class InterpretedThrowException : Exception
    {
        [CanBeNull]
        public IExp Throwed { get; }


        public InterpretedThrowException([CanBeNull] IExp throwed)
            : base(throwed.Accept(Program.DefaultPrinter))
        {
            Throwed = throwed;
        }
    }
}