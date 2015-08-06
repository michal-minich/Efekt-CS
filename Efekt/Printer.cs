using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Printer : IAsiVisitor<String>
    {
        public Boolean PutBracesAroundBinOpApply;


        public String VisitAsiList(AsiList al)
        {
            var sb = new StringBuilder();
            var counter = 0;
            foreach (var item in al.Items)
            {
                var itemStr = item.Accept(this);
                if (++counter != 1)
                    sb.Append('\n');
                sb.Append(itemStr);
            }
            return sb.ToString();
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
            return "[" + String.Join(", ", arr.Items.Select(i => i.Accept(this))) + "]";
        }


        public String VisitStruct(Struct s)
        {
            var b = String.Join(" ", s.Items.Select(i => i.Accept(this)));
            return b.Length == 0 ? "struct { }" : "struct { " + b + " }";
        }


        private String visitOptional([CanBeNull] IAsi asi, String prefix)
        {
            return asi == null ? "" : prefix + asi.Accept(this);
        }
    }
}