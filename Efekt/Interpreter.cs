using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Interpreter : IAsiVisitor<IAsi>
    {
        Env env;
        IAsi current;
        Env global;
        ValidationList validations;
        Builtins builtins;
        Boolean isReturn;
        Boolean isBreak;
        Boolean isContinue;
        IAsi start;


        public IAsi Eval(IReadOnlyList<IAsi> items, ValidationList validationList)
        {
            validations = validationList;
            builtins = new Builtins(validationList);
            var prog = new Class(new List<Declr>());
            global = env = new Env(validations, prog);
            prog.Env = global;
            var res = visitSeq(items, env, env);
            return res;
        }


        public IAsi Run(Prog prog, ValidationList validationList)
        {
            Eval(((Class)((New)prog.GlobalModule.Value).Exp).Items, validationList);
            var f = start as Fn;
            if (f == null)
                return start;
            var s = new FnApply(f, new List<Exp>());
            var res = s.Accept(this);
            start = null;
            return res;
        }


        public IAsi VisitSequence(Sequence seq)
        {
            return visitSeq(seq, new Env(validations, env.Owner), env);
        }


        public IAsi VisitInt(Int ii) => ii;


        public IAsi VisitIdent(Ident i)
        {
            if (i.Name == "this")
                return env.Owner;
            if (i.Name == "global")
                return global.Owner;
            if (i.Name.StartsWith("__"))
            {
                switch (i.Name)
                {
                    case "__Void":
                        return VoidType.Instance;
                    case "__Any":
                        return AnyType.Instance;
                    case "__Bool":
                        return BoolType.Instance;
                    case "__Int":
                        return IntType.Instance;
                    case "__Char":
                        return CharType.Instance;
                    case "__Arr":
                        return ArrType.Instance;
                    case "__Fn":
                        return FnType.Instance;
                    case "__Class":
                        return ClassType.Instance;
                }
            }
            var v = env.GetValueOrNull(i.Name);
            if (v != null)
                return v;
            validations.ImplicitVar(i);
            throw new UnexpectedException();
        }


        public IAsi VisitBinOpApply(BinOpApply opa)
        {
            switch (opa.Op.Name)
            {
                case ".":
                    return accessMember(opa);
                default:
                    return VisitFnApply(new FnApply(opa.Op, new List<Exp> { opa.Op1, opa.Op2 }));
            }
        }


        IAsi accessMember(BinOpApply opa)
        {
            var argValue = (Exp)opa.Op1.Accept(this);
            var member = (Ident)opa.Op2;
            if (argValue is Class)
            {
                var bag = getStructEnvOfMember(opa, argValue);
                var m = bag.GetOwnValueOrNull(member.Name);
                if (m != null)
                    return m;
            }
            return createMemberFn(member, argValue);
        }


        IAsi createMemberFn(Ident member, Exp argValue)
        {
            var mExt = env.GetValueOrNull(member.Name);
            if (mExt == null)
                validations.GenericWarning("member is unknown '{0}'", member);
            var mFn = mExt as Fn;
            if (mFn == null)
                validations.GenericWarning("member extension '{0}' must be a func", member);
            var extFn = new Fn(mFn.Params, mFn.Body)
            {
                Env = mFn.Env,
                CountMandatoryParams = mFn.CountMandatoryParams,
                Line = mFn.Line,
                ExtensionArg = argValue
            };
            return extFn;
        }


        Exp copyIfValue(Exp exp, IEnumerable<Exp> targetAttrs)
        {
            if (hasSimpleAttr(targetAttrs, "byref"))
                return exp;
            var a = copyIfcomplexValue(exp);
            a = copyIfArrayInstance(exp);
            return a;
        }


        Exp copyIfArrayInstance(Exp exp)
        {
            var a = exp as Arr;
            if (a == null)
                return exp;
            if (!a.IsEvaluated)
                throw new Exception("arr copy");
            var items = new List<Exp>();
            foreach (var item in a.Items)
            {
                items.Add(copyIfValue(item, new List<Exp>()));
            }
            return new Arr(items) { IsEvaluated = true };
        }


        static Exp copyIfcomplexValue(Exp exp)
        {
            return exp;
        }


        static Boolean hasSimpleAttr(IEnumerable<Exp> attrs, String name)
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

            var s2 = bag as Class;
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
                    "' of not constructed class", bag);
            return s2.Env;
        }


        public IAsi VisitDeclr(Declr d)
        {
            var acc = (env == global)
                ? Accessibility.Global
                : (current is Class ? Accessibility.Private : Accessibility.Local);

            if (d.Value == null)
            {
                if (hasSimpleAttr(d.Attributes, "public"))
                    env.Declare(Accessibility.Public, d.Ident.Name);
                else
                    env.Declare(acc, d.Ident.Name);
                return new Void();
            }

            var v = (Exp)d.Value.Accept(this);
            v = copyIfValue(v, new List<Exp>());
            if (hasSimpleAttr(d.Attributes, "public"))
                env.Declare(Accessibility.Public, d.Ident.Name, v);
            else
                env.Declare(acc, d.Ident.Name, v);

            if (d.Ident.Name == "start")
            {
                if (start != null)
                    validations.GenericWarning("Start is present multiple times.", Void.Instance);
                start = d.Value.Accept(this);
            }

            return v;
        }


        public IAsi VisitArr(Arr arr)
        {
            Contract.Assume(!arr.IsEvaluated);
            return new Arr(arr.Items
                              .Select(i => i.Accept(this))
                              .Cast<Exp>()
                              .ToList())
            { IsEvaluated = true };
        }


        public IAsi VisitClass(Class cls) => cls;


        public IAsi VisitFn(Fn fn)
            => new Fn(fn.Params, fn.Body)
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

                if (fnAsi is Class)
                    return new FnApply(fnAsi, fna.Args);
                fn = fnAsi as Fn;
                if (fn == null)
                {
                    validations.CannotApply(fna.Fn, fnAsi);
                    throw new UnexpectedException();
                }
            }

            current = fn;
            var prevEnv = env;
            var envForParams = new Env(validations, fn.Env.Owner, fn.Env);
            evalParamsAndArgs(fn, fna.Fn, fna.Args.ToArray(), envForParams);
            return visitSeq(fn.Body, envForParams, prevEnv);
        }


        Exp[] evalArgs(IEnumerable<Exp> args)
            => args.Select(arg => arg.Accept(this)).Cast<Exp>().ToArray();


        void evalParamsAndArgs(Fn fn, IAsi notEvaledFn, IReadOnlyList<Exp> args, Env envForParams)
        {
            IReadOnlyList<Exp> args2;
            if (args.Count < fn.CountMandatoryParams)
            {
                validations.NotEnoughArgs(fn.Params[args.Count], notEvaledFn, fn.Params.Count,
                                          fn.CountMandatoryParams, args.Count);
                var missingArgCount = fn.CountMandatoryParams - args.Count;
                if (missingArgCount != 0)
                    throw new UnexpectedException("missing arguments count: " + missingArgCount);
                //var errs = fn.Params.Skip(args.Count).Take(missingArgCount).Select(p => new Err(p));
                //args2 = args.Concat(errs).ToList();
                args2 = args;
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

            if (fn.ExtensionArg != null)
                evaluatedArgs = new[] { fn.ExtensionArg }.Concat(evaluatedArgs).ToArray();

            env = envForParams;
            foreach (var p in fn.Params)
            {
                if (evaluatedArgs.Length <= n)
                {
                    //p.Accept(this);
                    env.Declare(Accessibility.Local, p.Ident.Name, (Exp)p.Value?.Accept(this));
                }
                else
                {
                    var argValue = copyIfValue(evaluatedArgs[n], p.Attributes);
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
            var s = sAsi as Class;

            if (s == null)
            {
                validations.NoStructAfterNew(sAsi, sAsi.GetType().Name);
                throw new UnexpectedException();
            }
            if (s.Env != null)
            {
                validations.InstanceAfterNew(n.Exp);
                throw new UnexpectedException();
            }

            var prevEnv = env;
            var instance = /*s is Struct ? (IRecord)new Struct(new List<IAsi>()) :*/ new Class(new List<Declr>());
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


        IAsi visitSeq(IReadOnlyList<IAsi> items)
        {
            return visitSeq(items, new Env(validations, env.Owner, env), env);
        }


        IAsi visitSeq(IReadOnlyList<IAsi> items, Env newEnv, Env restoreEnv)
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
            return copyIfValue((Exp)r, new List<Exp>());
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
                ? visitSeq(iff.Then)
                : iff.Otherwise == null ? new Void() : visitSeq(iff.Otherwise);
        }


        public IAsi VisitAssign(Assign a)
        {
            var v = (Exp)a.Value.Accept(this);
            v = copyIfValue(v, new List<Exp>());
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

            validations.GenericWarning("Cannot assign to '{0}'", a.Target);
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
            return repeatWhile(() => true, rp.Sequence);
        }


        IAsi repeatWhile(Func<Boolean> condition, IReadOnlyCollection<IAsi> items,
                         String itemName = null)
        {
            IAsi r = Void.Instance;
            var prevEnv = env;
            env = new Env(validations, env.Owner, env);
            if (itemName != null)
                env.Declare(Accessibility.Local, itemName);
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
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
                    if (isBreak)
                    {
                        isBreak = false;
                        goto exit;
                    }
                    if (isContinue)
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
            }, fe.Sequence, fe.Ident.Name);
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
                var s = asi as Class;
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
            Exp throwed;
            if (th.Ex != null)
                throwed = (Exp)th.Ex.Accept(this);
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
                visitSeq(tr.TrySequence, env, prevEnv);
            }
            catch (InterpretedThrowException ex)
            {
                if (tr.CatchSequence != null)
                {
                    env = new Env(validations, env.Owner, env);
                    if (tr.ExVar != null)
                        env.Declare(Accessibility.Local, tr.ExVar.Name, ex.Throwed);
                    visitSeq(tr.CatchSequence, env, prevEnv);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                if (tr.FinallySequence != null)
                {
                    env = new Env(validations, env.Owner, env);
                    visitSeq(tr.FinallySequence, env, prevEnv);
                }
            }
            return Void.Instance;
        }


        static Arr toCharArr(String value)
            => new Arr(value.Select(ch => new Char(ch)).Cast<Exp>().ToList());


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


        public IAsi VisitSimpleType(SimpleType st) => st;
    }


    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [Serializable]
    public sealed class InterpretedThrowException : Exception
    {
        [CanBeNull]
        public Exp Throwed { get; }


        public InterpretedThrowException([CanBeNull] Exp throwed)
            : base(throwed.Accept(Program.DefaultPrinter))
        {
            Throwed = throwed;
        }
    }
}