using System;


namespace Efekt
{
    public sealed class Namer : IAsiVisitor<Void>
    {
        Env env;
        Struct currentStruct;
        ValidationList validations;


        public void Name(Struct prog, ValidationList validationsList)
        {
            validations = validationsList;
            env = new Env(validations, prog);
            foreach (var item in prog.Items)
                item.Accept(this);
            currentStruct = null;
            env = null;
        }


        public Void VisitAsiList(AsiList al)
        {
            var prevEnv = env;
            env = new Env(validations, currentStruct, env);
            foreach (var item in al.Items)
                item.Accept(this);
            env = prevEnv;
            return Void.Instance;
        }


        public Void VisitErr(Err err)
        {
            err.Item?.Accept(this);
            return Void.Instance;
        }


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
                env.Declare(Accessibility.Public, x.Name, x);
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
            env.Declare(Accessibility.Public, d.Ident.Name, d.Ident);
            d.Type?.Accept(this);
            return Void.Instance;
        }


        public Void VisitArr(Arr arr)
        {
            foreach (var item in arr.Items)
                item.Accept(this);
            return Void.Instance;
        }


        public Void VisitStruct(Struct s)
        {
            var prevStruct = currentStruct;
            var prevEnv = env;
            currentStruct = s;
            env = new Env(validations, s, env);
            foreach (var item in s.Items)
                item.Accept(this);
            currentStruct = prevStruct;
            env = prevEnv;
            return Void.Instance;
        }


        public Void VisitClass(Class cls)
        {
            throw new NotImplementedException();
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
            iff.Then.Accept(this);
            iff.Otherwise?.Accept(this);
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
            throw new NotImplementedException();
        }


        public Void VisitTry(Try tr)
        {
            throw new NotImplementedException();
        }


        public Void VisitAssume(Assume asm)
        {
            throw new NotImplementedException();
        }


        public Void VisitAssert(Assert ast)
        {
            throw new NotImplementedException();
        }


        public Void VisitSimpleType(ISimpleType st)
        {
            throw new NotImplementedException();
        }
    }
}