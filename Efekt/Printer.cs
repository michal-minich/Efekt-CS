﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Printer : IAsiVisitor<String>
    {
        public Boolean PutBracesAroundBinOpApply { private get; set; }


        String visitSeq(IReadOnlyList<IAsi> items) => joinStatements(items);


        public String VisitSequence(Sequence seq)
        {
            return visitSeq(seq);
        }


        public String VisitInt(Int ii) => ii.Value.ToString();


        public String VisitIdent(Ident i)
            => (i.Category == IdentCategory.Op ? "op" : "") + i.Name;


        public String VisitBinOpApply(BinOpApply opa)
        {
            var str = opa.Op1.Accept(this) + " " + opa.Op.Name + " " + opa.Op2.Accept(this);
            return PutBracesAroundBinOpApply ? "(" + str + ")" : str;
        }


        public String VisitDeclr(Declr d)
            => (d.IsVar ? "var " : "") + VisitIdent(d.Ident) + visitOptional(d.Type, " : ")
               + visitOptional(d.Value, " = ");


        public String VisitArr(Arr arr)
        {
            if (arr.Items.Any() && arr.Items.First() is Char)
                return "\"" + String.Join("", arr.Items.Cast<Char>().Select(ch => ch.Value)) + "\"";
            return "[" + joinList(arr.Items) + "]";
        }


        public String VisitClass(Class cls) => printRecord(cls, "class");


        String printRecord(Class r, String name)
        {
            var b = joinStatements(r.Items);
            return b.Length == 0 ? name + " { }" : name + " { " + b + " }";
        }


        public String VisitFn(Fn fn)
        {
            var b = joinStatementsOneLine(fn.Body);
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
            "if " + iff.Test.Accept(this) + " then " + visitSeq(iff.Then)
            + (iff.Otherwise == null ? "" : " else " + visitSeq(iff.Otherwise));


        public String VisitAssign(Assign a)
        {
            var str = a.Target.Accept(this) + " = " + a.Value.Accept(this);
            return PutBracesAroundBinOpApply ? "(" + str + ")" : str;
        }


        public String VisitImport(Import imp) => "import " + imp.QualifiedIdent.Accept(this);


        public String VisitGoto(Goto gt) => "goto " + gt.LabelName.Name;


        public String VisitLabel(Label lbl) => "label " + lbl.LabelName.Name;


        public String VisitBreak(Break br)
            => "break" + (br.Test == null ? "" : " if " + br.Test.Accept(this));


        public String VisitContinue(Continue ct)
            => "continue" + (ct.Test == null ? "" : " if " + ct.Test.Accept(this));


        public String VisitReturn(Return r)
            => "return" + (r.Value == null ? "" : " " + r.Value.Accept(this));


        public String VisitRepeat(Repeat rp) => "repeat {" + joinStatements(rp.Sequence) + "}";


        public String VisitForEach(ForEach fe)
            => "foreach " + fe.Ident.Name + " in " + fe.Iterable.Accept(this)
               + " {" + joinStatements(fe.Sequence) + "}";


        public String VisitThrow(Throw th)
            => "throw" + (th.Ex == null ? "" : " " + th.Ex.Accept(this));


        public String VisitTry(Try tr)
        {
            var s = "try " + " {" + joinStatements(tr.TrySequence) + "}";
            if (tr.CatchSequence != null)
                s += "\ncatch " + " {" + joinStatements(tr.CatchSequence) + "}";
            if (tr.FinallySequence != null)
                s += "\nfinally " + " {" + joinStatements(tr.FinallySequence) + "}";
            return s;
        }


        public String VisitAssume(Assume asm) => "assume " + asm.Exp.Accept(this);

        public String VisitAssert(Assert ast) => "assert " + ast.Exp.Accept(this);


        public String VisitSimpleType(SimpleType st) => st.Name;


        public String VisitFnType(FnType fnt)
            => "Fn(" + joinList(fnt.ParamTypes) + ") -> " + fnt.ReturnType.Accept(this);


        public String VisitClassType(ClassType clst) => VisitIdent(clst.DeclaredBy.Ident);

        public String VisitOrType(OrType ort) => "Or(" + joinList(ort.Choices) + ")";

        public String VisitArrType(ArrType arrt) => "Array(" + arrt.ElementType.Accept(this) + ")";
    }
}