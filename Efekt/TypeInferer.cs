using System;
using System.Collections.Generic;


namespace Efekt
{
    public sealed class TypeInferer : IAsiVisitor<IExp>
    {
        public IExp VisitAsiList(AsiList al)
        {
            throw new NotImplementedException();
        }


        public IExp VisitErr(Err err) => ErrType.Instance;

        public IExp VisitInt(Int ii) => IntType.Instance;


        public IExp VisitIdent(Ident i)
        {
            throw new NotImplementedException();
        }


        public IExp VisitBinOpApply(BinOpApply opa)
        {
            var t = VisitFnApply(new FnApply(opa.Op, new List<IExp> { opa.Op1, opa.Op2 }));
            return t;
        }


        public IExp VisitDeclr(Declr d)
        {
            if (d.Type != null)
                return d.Type;
            if (d.Value != null)
                return d.Value.Accept(this);
            return AnyType.Instance;
        }


        public IExp VisitArr(Arr arr) => ArrType.Instance;


        public IExp VisitStruct(Struct s)
        {
            throw new NotImplementedException();
        }


        public IExp VisitClass(Class cls) => ClassType.Instance;

        public IExp VisitFn(Fn fn) => FnType.Instance;


        public IExp VisitFnApply(FnApply fna)
        {
            throw new NotImplementedException();
        }


        public IExp VisitNew(New n) => n.Exp.Accept(this);

        public IExp VisitVoid(Void v) => VoidType.Instance;

        public IExp VisitBool(Bool b) => BoolType.Instance;

        public IExp VisitChar(Char c) => CharType.Instance;


        public IExp VisitIf(If iff)
        {
            var t = iff.Then.Accept(this);
            if (iff.Otherwise == null)
                return t;
            var o = iff.Otherwise.Accept(this);
            return t == o ? t : AnyType.Instance;
        }


        public IExp VisitImport(Import imp) => VoidType.Instance;

        public IExp VisitAssign(Assign a) => a.Value.Accept(this);

        public IExp VisitGoto(Goto gt) => VoidType.Instance;

        public IExp VisitLabel(Label lbl) => VoidType.Instance;

        public IExp VisitBreak(Break br) => VoidType.Instance;

        public IExp VisitContinue(Continue ct) => VoidType.Instance;

        public IExp VisitReturn(Return r) => r.Value == null ? VoidType.Instance : r.Value.Accept(this);

        public IExp VisitRepeat(Repeat rp) => VoidType.Instance;

        public IExp VisitForEach(ForEach fe) => VoidType.Instance;

        public IExp VisitThrow(Throw th) => VoidType.Instance;

        public IExp VisitTry(Try tr) => VoidType.Instance;

        public IExp VisitAssume(Assume asm) => VoidType.Instance;

        public IExp VisitAssert(Assert ast) => VoidType.Instance;


        public IExp VisitSimpleType(ISimpleType st)
        {
            throw new NotSupportedException("type of type...");
        }
    }
}