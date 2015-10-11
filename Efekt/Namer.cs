using System;
using System.Collections.Generic;
using System.Linq;


namespace Efekt
{
    public sealed class Namer : IAsiVisitor<Void>
    {
        SimpleEnv<Declr> env;
        ValidationList validations;
        String declrName;
        readonly Dictionary<String, SimpleEnv<Declr>> classEnvs = new Dictionary<String, SimpleEnv<Declr>>();


        public void Name(Prog prog, ValidationList validationsList)
        {
            validations = validationsList;
            env = new SimpleEnv<Declr>();
            VisitDeclr(prog.GlobalModule);
            env = null;
            validations = null;
        }


        public Void VisitSequence(Sequence seq)
        {
            var prevEnv = env;
            env = new SimpleEnv<Declr>(env);
            foreach (var item in seq)
                item.Accept(this);
            env = prevEnv;
            return Void.Instance;
        }


        public Void VisitInt(Int ii)
        {
            return Void.Instance;
        }


        public Void VisitIdent(Ident i)
        {
            if (i.Name.StartsWith("__"))
                return Void.Instance;
            var d = env.GetValueOrNull(i.Name);
            if (d == null)
            {
                validations.ImplicitVar(i);
                var x = new Declr(new Ident(i.Name, IdentCategory.Value), null, Void.Instance);
                env.Declare(x.Ident.Name, x);
                d = x;
            }
            i.DeclaredBy = d;
            i.DeclaredBy.Ident.UsedBy.Add(i);
            return Void.Instance;
        }


        public Void VisitBinOpApply(BinOpApply opa)
        {
            switch (opa.Op.Name)
            {
                case ".":
                    opa.Op1.Accept(this);
                    break;
                default:
                    VisitFnApply(new FnApply(opa.Op, new List<Exp> { opa.Op1, opa.Op2 }));
                    break;
            }
            return Void.Instance;
        }


        public Void VisitDeclr(Declr d)
        {
            var prevDeclrName = declrName;
            declrName = d.Ident.Name;
            d.Type?.Accept(this);
            d.Value?.Accept(this);
            env.Declare(d.Ident.Name, d);
            declrName = prevDeclrName;
            return Void.Instance;
        }


        public Void VisitArr(Arr arr)
        {
            foreach (var item in arr.Items)
                item.Accept(this);
            return Void.Instance;
        }


        public Void VisitClass(Class cls)
        {
            var prevEnv = env;
            env = new SimpleEnv<Declr>(env);

            if (declrName != null)
                classEnvs.Add(declrName, env);

            foreach (var imp in cls.Items.TakeWhile(c => c is Import))
                VisitImport((Import)imp);

            var declrs = cls.Items.OfType<Declr>().ToList();

            foreach (var d in declrs)
                env.Declare(d.Ident.Name, d);

            var prevDeclrName = declrName;
            foreach (var d in declrs)
            {
                declrName = d.Ident.Name;
                d.Type?.Accept(this);
                d.Value?.Accept(this);
            }
            declrName = prevDeclrName;
            env = prevEnv;
            return Void.Instance;
        }


        public Void VisitFn(Fn fn)
        {
            var prevEnv = env;
            env = new SimpleEnv<Declr>(env);
            foreach (var item in fn.Params)
                item.Accept(this);
            VisitSequence(fn.Body);
            env = prevEnv;
            return Void.Instance;
        }


        public Void VisitFnApply(FnApply fna)
        {
            fna.Fn.Accept(this);
            foreach (var item in fna.Args)
                item.Accept(this);
            return Void.Instance;
        }


        public Void VisitNew(New n)
        {
            n.Exp.Accept(this);
            return Void.Instance;
        }


        public Void VisitVoid(Void v)
        {
            return Void.Instance;
        }


        public Void VisitBool(Bool b)
        {
            return Void.Instance;
        }


        public Void VisitChar(Char c)
        {
            return Void.Instance;
        }


        public Void VisitIf(If iff)
        {
            iff.Test.Accept(this);
            VisitSequence(iff.Then);
            if (iff.Otherwise != null)
                VisitSequence(iff.Otherwise);
            return Void.Instance;
        }


        public Void VisitImport(Import imp)
        {
            var name = ((Ident)imp.QualifiedIdent).Name;
            var d = env.GetValueOrNull(name);
            env.AddImport(name, classEnvs[d.Ident.Name]);
            return Void.Instance;
        }


        public Void VisitAssign(Assign a)
        {
            a.Target.Accept(this);
            a.Value.Accept(this);
            return Void.Instance;
        }


        public Void VisitGoto(Goto gt)
        {
            throw new NotImplementedException();
        }


        public Void VisitLabel(Label lbl)
        {
            throw new NotImplementedException();
        }


        public Void VisitBreak(Break br)
        {
            return Void.Instance;
        }


        public Void VisitContinue(Continue ct)
        {
            return Void.Instance;
        }


        public Void VisitReturn(Return r)
        {
            r.Value?.Accept(this);
            return Void.Instance;
        }


        public Void VisitRepeat(Repeat rp)
        {
            VisitSequence(rp.Sequence);
            return Void.Instance;
        }


        public Void VisitForEach(ForEach fe)
        {
            fe.Ident.Accept(this);
            fe.Iterable.Accept(this);
            VisitSequence(fe.Sequence);
            return Void.Instance;
        }


        public Void VisitThrow(Throw th)
        {
            th.Ex?.Accept(this);
            return Void.Instance;
        }


        public Void VisitTry(Try tr)
        {
            VisitSequence(tr.TrySequence);
            VisitSequence(tr.CatchSequence);
            VisitSequence(tr.FinallySequence);
            return Void.Instance;
        }


        public Void VisitAssume(Assume asm)
        {
            asm.Exp.Accept(this);
            return Void.Instance;
        }


        public Void VisitAssert(Assert ast)
        {
            ast.Exp.Accept(this);
            return Void.Instance;
        }


        public Void VisitSimpleType(SimpleType st)
        {
            return Void.Instance;
        }
    }
}