using System;
using System.Collections.Generic;
using System.Linq;


namespace Efekt
{
    public sealed class TypeInferer : IAsiVisitor<Type>
    {
        public Type VisitSequence(Sequence seq)
        {
            if (seq.InfType != null)
                return seq.InfType;
            foreach (var item in seq.DropLast())
                item.Accept(this);
            seq.InfType = seq.Last().Accept(this);
            return seq.InfType;
        }


        public Type VisitInt(Int ii)
        {
            ii.InfType = IntType.Instance;
            return ii.InfType;
        }


        public Type VisitIdent(Ident i)
        {
            if (i.InfType != null)
                return i.InfType;
            if (i.Name.StartsWith("__"))
            {
                switch (i.Name)
                {
                    case "__Void":
                        i.InfType = VoidType.Instance;
                        break;
                    case "__Any":
                        i.InfType = AnyType.Instance;
                        break;
                    case "__Bool":
                        i.InfType = BoolType.Instance;
                        break;
                    case "__Int":
                        i.InfType = IntType.Instance;
                        break;
                    case "__Char":
                        i.InfType = CharType.Instance;
                        break;
                    default:
                        i.InfType = new FnType(new List<Type>(), AnyType.Instance);
                        break;
                }
            }
            else
            {
                i.InfType = i.DeclaredBy.Accept(this);
            }
            return i.InfType;
        }


        public Type VisitBinOpApply(BinOpApply opa)
        {
            switch (opa.Op.Name)
            {
                case ".":
                    return opa.Op1.InfType = opa.Op1.Accept(this);
                default:
                    if (opa.InfType != null)
                        return opa.InfType;
                    opa.InfType = VisitFnApply(new FnApply(opa.Op, new List<Exp> { opa.Op1, opa.Op2 }));
                    return opa.InfType;
            }
        }


        public Type VisitDeclr(Declr d)
        {
            if (d.InfType != null)
                return d.InfType;
            if (d.Type != null)
                d.InfType = evalTypeExp(d.Type);
            else if (d.Value != null)
                d.InfType = d.Value.Accept(this);
            else
                d.InfType = AnyType.Instance;
            d.Ident.InfType = d.InfType;
            return d.InfType;
        }


        static Type evalTypeExp(Exp exp)
        {
            return (Type)exp;
        }


        public Type VisitArr(Arr arr)
        {
            if (arr.InfType != null)
                return arr.InfType;
            Type commonType;
            if (arr.Items.Count == 0)
            {
                commonType = AnyType.Instance;
            }
            else
            {
                var types = arr.Items
                               .Select(i => i.Accept(this))
                               .Distinct(new EqComparer<Type>((x, y) => x.AsiToString() == y.AsiToString()))
                               .ToList();
                commonType = types.Count == 1 ? types[0] : new OrType(types);
            }
            arr.InfType = new ArrType(commonType);
            return arr.InfType;
        }


        public Type VisitClass(Class cls)
        {
            if (cls.InfType != null)
                return cls.InfType;
            foreach (var item in cls.Items)
                item.Accept(this);
            cls.InfType = new ClassType(null);
            return cls.InfType;
        }


        public Type VisitFn(Fn fn)
        {
            if (fn.InfType != null)
                return fn.InfType;
            fn.InfType = new FnType(fn.Params.Select(p => p.Accept(this)).ToList(), VisitSequence(fn.Body));
            return fn.InfType;
        }


        public Type VisitFnApply(FnApply fna)
        {
            if (fna.InfType != null)
                return fna.InfType;
            foreach (var arg in fna.Args)
                arg.InfType = arg.Accept(this);
            fna.InfType = ((FnType)fna.Fn.Accept(this)).ReturnType;
            return fna.InfType;
        }


        public Type VisitNew(New n)
        {
            if (n.InfType != null)
                return n.InfType;
            n.InfType = n.Exp.Accept(this);
            return n.InfType;
        }


        public Type VisitVoid(Void v)
        {
            v.InfType = VoidType.Instance;
            return v.InfType;
        }


        public Type VisitBool(Bool b)
        {
            b.InfType = BoolType.Instance;
            return b.InfType;
        }


        public Type VisitChar(Char c)
        {
            c.InfType = CharType.Instance;
            return c.InfType;
        }


        public Type VisitIf(If iff)
        {
            if (iff.InfType != null)
                return iff.InfType;
            iff.Test.Accept(this);
            var t = VisitSequence(iff.Then);
            if (iff.Otherwise == null)
                return t;
            var o = VisitSequence(iff.Otherwise);
            iff.InfType = t == o ? t : new OrType(new List<Type> { t, o });
            return iff.InfType;
        }


        public Type VisitImport(Import imp) => VoidType.Instance;

        public Type VisitAssign(Assign a) => a.Value.Accept(this);

        public Type VisitGoto(Goto gt) => VoidType.Instance;

        public Type VisitLabel(Label lbl) => VoidType.Instance;

        public Type VisitBreak(Break br) => VoidType.Instance;

        public Type VisitContinue(Continue ct) => VoidType.Instance;

        public Type VisitReturn(Return r) => r.Value == null ? VoidType.Instance : r.Value.Accept(this);

        public Type VisitRepeat(Repeat rp) => VoidType.Instance;

        public Type VisitForEach(ForEach fe) => VoidType.Instance;

        public Type VisitThrow(Throw th) => VoidType.Instance;

        public Type VisitTry(Try tr) => VoidType.Instance;

        public Type VisitAssume(Assume asm) => VoidType.Instance;

        public Type VisitAssert(Assert ast) => VoidType.Instance;


        public Type VisitSimpleType(SimpleType st)
        {
            throw new NotSupportedException("type of type...");
        }


        public Type VisitFnType(FnType fnt)
        {
            throw new NotSupportedException("type of type fn...");
        }


        public Type VisitClassType(ClassType clst)
        {
            throw new NotSupportedException("type of type class...");
        }


        public Type VisitOrType(OrType ort)
        {
            throw new NotSupportedException("type of type or...");
        }


        public Type VisitArrType(ArrType arrt)
        {
            throw new NotSupportedException("type of type array...");
        }
    }
}