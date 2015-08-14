using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Printer : IAsiVisitor<String>
    {
        public Boolean PutBracesAroundBinOpApply { get; set; }


        public String VisitAsiList(AsiList al) => joinStatements(al.Items);

        public String VisitErr(Err err) => "error (" + err.Accept(this) + ")";


        public String VisitInt(Int ii) => ii.Value;


        public String VisitIdent(Ident i)
            => (i.Category == IdentCategory.Op ? "op" : "") + i.Name;


        public String VisitBinOpApply(BinOpApply opa)
        {
            var str = opa.Op1.Accept(this) + " " + opa.Op.Name + " " + opa.Op2.Accept(this);
            return PutBracesAroundBinOpApply ? "(" + str + ")" : str;
        }


        public String VisitDeclr(Declr d)
            => (d.IsVar ? "var " : "") + VisitIdent(d.Ident) + visitOptional(d.Type, " : ");


        public String VisitArr(Arr arr)
        {
            if (arr.Items.Any() && arr.Items.First() is Char)
                return "\"" + String.Join("", arr.Items.Cast<Char>().Select(ch => ch.Value)) + "\"";
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
            => fna.Fn.Accept(this) + "(" + joinList(fna.Args) + ")";


        public String VisitNew(New n) => "new " + n.Exp.Accept(this);


        public String VisitVoid(Void v) => "void";


        private String joinStatementsOneLine(IEnumerable<IAsi> items)
        {
            return String.Join(" ", items.Select(i => i.Accept(this)));
        }


        private String joinStatements(IEnumerable<IAsi> items)
        {
            return String.Join("\n", items.Select(i => i.Accept(this)));
        }


        private String joinList(IEnumerable<IAsi> items)
        {
            return String.Join(", ", items.Select(i => i.Accept(this)));
        }


        private String visitOptional([CanBeNull] IAsi asi, String prefix)
            => asi == null ? "" : prefix + asi.Accept(this);


        public String VisitBool(Bool b) => b.Value ? "true" : "false";

        public String VisitChar(Char c) => "'" + c.Value.ToString() + "'";


        public String VisitIf(If iff)
        {
            return "if " + iff.Test.Accept(this) + " then " + iff.Test.Accept(this)
                   + (iff.Otherwise == null ? "" : " else " + iff.Otherwise.Accept(this));
        }


        public String VisitImport(Import imp) => "import " + imp.QualifiedIdent.Accept(this);
    }
}