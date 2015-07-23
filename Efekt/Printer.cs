using System;
using System.Text;


namespace Efekt
{
    public sealed class Printer : IAsiVisitor<String>
    {
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
            return opa.Op1.Accept(this) + " " + opa.Op.Value + " " + opa.Op2.Accept(this);
        }
    }
}