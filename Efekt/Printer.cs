using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Printer : IAsiVisitor<String>
    {
        public Boolean PutBracesAroundBinOpApply { private get; set; }


        public String VisitAsiList(AsiList al) => joinStatements(al.Items);


        public String VisitErr(Err err)
            => "error (" + (err.Item == null ? "" : err.Item.Accept(this)) + ")";


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
            return "fn " + joinList(fn.Params) + (fn.Params.Count == 0 ? "" : " ")
                   + (b.Length == 0 ? "{ }" : "{ " + b + " }");
        }


        public String VisitFnApply(FnApply fna)
            => fna.Fn.Accept(this) + "(" + joinList(fna.Args) + ")";


        public String VisitNew(New n) => "new " + n.Exp.Accept(this);


        public String VisitVoid(Void v) => "void";


        String joinStatementsOneLine(IEnumerable<IAsi> items)
            => String.Join(" ", items.Select(i => i.Accept(this)));


        String joinStatements(IEnumerable<IAsi> items)
            => String.Join("\n", items.Select(i => i.Accept(this)));


        String joinList(IEnumerable<IAsi> items)
        {
            return String.Join(", ", items.Select(i => i.Accept(this)));
        }


        String visitOptional([CanBeNull] IAsi asi, String prefix)
            => asi == null ? "" : prefix + asi.Accept(this);


        public String VisitBool(Bool b) => b.Value ? "true" : "false";

        public String VisitChar(Char c) => "'" + c.Value + "'";


        public String VisitIf(If iff) =>
            "if " + iff.Test.Accept(this) + " then " + iff.Test.Accept(this)
            + (iff.Otherwise == null ? "" : " else " + iff.Otherwise.Accept(this));


        public String VisitAssign(Assign a)
        {
            var str = a.Target.Accept(this) + " = " + a.Value.Accept(this);
            return PutBracesAroundBinOpApply ? "(" + str + ")" : str;
        }


        public String VisitImport(Import imp) => "import " + imp.QualifiedIdent.Accept(this);


        public String VisitGoto(Goto gt) => "goto " + gt.LabelName.Name;


        public String VisitLabel(Label lbl) => "label " + lbl.LabelName.Name;


        public String VisitBreak(Break br)
            => "break" + (br.LabelName == null ? "" : " " + br.LabelName.Name);


        public String VisitContinue(Continue ct)
            => "continue" + (ct.LabelName == null ? "" : " " + ct.LabelName.Name);


        public String VisitReturn(Return r)
            => "return" + (r.Value == null ? "" : " " + r.Value.Accept(this));


        public String VisitRepeat(Repeat rp) => "repeat {" + joinStatements(rp.Items) + "}";


        public String VisitForEach(ForEach fe)
            => "foreach " + fe.Ident.Name + " in " + fe.Iterable.Accept(this)
               + " {" + joinStatements(fe.Items);
    }
}