using System;
using System.Collections.Generic;


namespace Efekt
{
    public sealed class TypeInferer : IAsiVisitor<Exp>
    {
        Exp visitSeq(IReadOnlyList<IAsi> list)
        {
            throw new NotImplementedException();
        }


        public Exp VisitSequence(Sequence seq)
        {
            return visitSeq(seq);
        }


        public Exp VisitInt(Int ii) => IntType.Instance;


        public Exp VisitIdent(Ident i)
        {
            throw new NotImplementedException();
        }


        public Exp VisitBinOpApply(BinOpApply opa)
        {
            var t = VisitFnApply(new FnApply(opa.Op, new List<Exp> { opa.Op1, opa.Op2 }));
            return t;
        }


        public Exp VisitDeclr(Declr d)
        {
            if (d.Type != null)
                return d.Type;
            if (d.Value != null)
                return d.Value.Accept(this);
            return AnyType.Instance;
        }


        public Exp VisitArr(Arr arr) => ArrType.Instance;


        public Exp VisitClass(Class cls) => ClassType.Instance;

        public Exp VisitFn(Fn fn) => FnType.Instance;


        public Exp VisitFnApply(FnApply fna)
        {
            throw new NotImplementedException();
        }


        public Exp VisitNew(New n) => n.Exp.Accept(this);

        public Exp VisitVoid(Void v) => VoidType.Instance;

        public Exp VisitBool(Bool b) => BoolType.Instance;

        public Exp VisitChar(Char c) => CharType.Instance;


        public Exp VisitIf(If iff)
        {
            var t = visitSeq(iff.Then);
            if (iff.Otherwise == null)
                return t;
            var o = visitSeq(iff.Otherwise);
            return t == o ? t : AnyType.Instance;
        }


        public Exp VisitImport(Import imp) => VoidType.Instance;

        public Exp VisitAssign(Assign a) => a.Value.Accept(this);

        public Exp VisitGoto(Goto gt) => VoidType.Instance;

        public Exp VisitLabel(Label lbl) => VoidType.Instance;

        public Exp VisitBreak(Break br) => VoidType.Instance;

        public Exp VisitContinue(Continue ct) => VoidType.Instance;

        public Exp VisitReturn(Return r) => r.Value == null ? VoidType.Instance : r.Value.Accept(this);

        public Exp VisitRepeat(Repeat rp) => VoidType.Instance;

        public Exp VisitForEach(ForEach fe) => VoidType.Instance;

        public Exp VisitThrow(Throw th) => VoidType.Instance;

        public Exp VisitTry(Try tr) => VoidType.Instance;

        public Exp VisitAssume(Assume asm) => VoidType.Instance;

        public Exp VisitAssert(Assert ast) => VoidType.Instance;


        public Exp VisitSimpleType(SimpleType st)
        {
            throw new NotSupportedException("type of type...");
        }
    }
}