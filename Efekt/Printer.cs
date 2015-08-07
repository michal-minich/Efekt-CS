using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Printer : IAsiVisitor<String>
    {
        public Boolean PutBracesAroundBinOpApply;


        public String VisitAsiList(AsiList al)
        {
            return joinStatements(al.Items);
        }


        public String VisitInt(Int ii)
        {
            return ii.Value;
        }


        public String VisitIdent(Ident ident)
        {
            return (ident.Type == IdentType.Op ? "op" : "") + ident.Value;
        }


        public String VisitBinOpApply(BinOpApply opa)
        {
            var str = opa.Op1.Accept(this) + " " + opa.Op.Value + " " + opa.Op2.Accept(this);
            return PutBracesAroundBinOpApply ? "(" + str + ")" : str;
        }


        public String VisitDeclr(Declr d)
        {
            return VisitIdent(d.Ident) + visitOptional(d.Type, " : ") + visitOptional(d.Value, " = ");
        }


        public String VisitArr(Arr arr)
        {
            return "[" + joinList(arr.Items) + "]";
        }


        public String VisitStruct(Struct s)
        {
            var b = joinStatements(s.Items);
            return b.Length == 0 ? "struct { }" : "struct { " + b + " }";
        }


        public String VisitFn(Fn fn)
        {
            var b = joinStatementsOneLine(fn.Items);
            return "fn (" + joinList(fn.Params) + ") " + (b.Length == 0 ? "{ }" : "{ " + b + " }");
        }


        public String VisitFnApply(FnApply fna)
        {
            return fna.Fn.Accept(this) + "(" + joinList(fna.Args) + ")";
        }


        private String joinStatementsOneLine(IEnumerable<Asi> items)
        {
            return String.Join(" ", items.Select(i => i.Accept(this)));
        }


        private String joinStatements(IEnumerable<Asi> items)
        {
            return String.Join("\n", items.Select(i => i.Accept(this)));
        }


        private String joinList(IEnumerable<Asi> items)
        {
            return String.Join(", ", items.Select(i => i.Accept(this)));
        }


        private String visitOptional([CanBeNull] IAsi asi, String prefix)
        {
            return asi == null ? "" : prefix + asi.Accept(this);
        }
    }
}