using System;
using System.Collections.Generic;


namespace Efekt
{
    public sealed class Namer : IAsiVisitor<Void>
    {
        SimpleEnv env;
        Class currentClass;
        ValidationList validations;


        public void Name(Prog prog, ValidationList validationsList)
        {
            validations = validationsList;
            env = new SimpleEnv();
            foreach (var item in prog.Modules)
                item.Accept(this);
            currentClass = null;
            env = null;
        }


        public Void visitSeq(IReadOnlyList<IAsi> items)
        {
            var prevEnv = env;
            env = new SimpleEnv(env);
            foreach (var item in items)
                item.Accept(this);
            env = prevEnv;
            return Void.Instance;
        }


        public Void VisitSequence(Sequence seq) => visitSeq(seq);


        public Void VisitInt(Int ii)
        {
            return Void.Instance;
        }


        public Void VisitIdent(Ident i)
        {
            var declaredBy = env.GetValueOrNull(i.Name);
            if (declaredBy == null)
            {
                validations.ImplicitVar(i);
                var x = new Ident(i.Name, IdentCategory.Value);
                env.Declare(x.Name, x);
                declaredBy = x;
            }
            if (declaredBy is Label)
                throw new EfektException(
                    "expected variable instead of label with the name: " + i.Name);
            var declaredByIdent = (Ident)declaredBy;
            i.DeclaredBy = null;
            declaredByIdent.UsedBy.Add(i);
            return Void.Instance;
        }


        public Void VisitBinOpApply(BinOpApply opa)
        {
            opa.Op.Accept(this);
            opa.Op1.Accept(this);
            opa.Op2.Accept(this);
            return Void.Instance;
        }


        public Void VisitDeclr(Declr d)
        {
            env.Declare(d.Ident.Name, d.Ident);
            d.Type?.Accept(this);
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
            var prevStruct = currentClass;
            var prevEnv = env;
            currentClass = cls;
            env = new SimpleEnv(env);
            foreach (var item in cls.Items)
                item.Accept(this);
            currentClass = prevStruct;
            env = prevEnv;
            return Void.Instance;
        }


        public Void VisitFn(Fn fn)
        {
            foreach (var item in fn.Params)
                item.Accept(this);
            foreach (var item in fn.BodyItems)
                item.Accept(this);
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
            visitSeq(iff.Then);
            if (iff.Otherwise != null)
                visitSeq(iff.Otherwise);
            return Void.Instance;
        }


        public Void VisitImport(Import imp)
        {
            imp.QualifiedIdent.Accept(this);
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
            var a = env.GetValueOrNull(gt.LabelName.Name);
            var i = a as Ident;
            if (i != null)
                throw new EfektException("expected label, got variable: " + i.Name);
            var l = (Label)a;
            //gt.LabelName.DeclaredBy = l.LabelName;
            l.LabelName.UsedBy.Add(gt.LabelName);
            return Void.Instance;
        }


        public Void VisitLabel(Label lbl)
        {
            // env.Declare(lbl.LabelName.Name, lbl);
            return Void.Instance;
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
            throw new NotImplementedException();
        }


        public Void VisitForEach(ForEach fe)
        {
            fe.Ident.Accept(this);
            fe.Iterable.Accept(this);
            foreach (var item in fe.Items)
                item.Accept(this);
            return Void.Instance;
        }


        public Void VisitThrow(Throw th)
        {
            th.Ex?.Accept(this);
            return Void.Instance;
        }


        public Void VisitTry(Try tr)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}