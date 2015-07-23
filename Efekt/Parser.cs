using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;


namespace Efekt
{
    public sealed class Parser
    {
        private String code;
        private Int32 index;
        private String matched = "";


        public Asi Parse(String codeText)
        {
            Contract.Requires(codeText != null);

            code = codeText;
            index = 0;
            var items = new List<Asi>();
            while (true)
            {
                var asi = parseCombinedAsi();
                if (asi == null)
                    break;
                items.Add(asi);
            }

            return new AsiList(items);
        }


        private Asi parseCombinedAsi()
        {
            var asi = parseAsi();
            if (asi == null)
                return null;
            Boolean found;
            do
            {
                found = false;
                skipWhite();
                if (matchUntil(isOp))
                {
                    asi = parseOpApply(asi);
                    found = true;
                }
            } while (found);
            return asi;
        }


        private Asi parseAsi()
        {
            skipWhite();
            var asi = (Asi) parseInt() ?? parseIdent();
            return asi;
        }


        private Int parseInt()
        {
            return matchUntil(isDigit) ? new Int(matched) : null;
        }


        private Ident parseIdent()
        {
            var isLetterMatched = matchUntil(isLetter);
            if (!isLetterMatched)
                return null;

            if (matched != "op")
                return new Ident(matched, Char.IsUpper(matched[0]) ? IdentType.Type : IdentType.Value);

            var isOpMatched = matchUntil(isOp);
            return isOpMatched ? new Ident(matched, IdentType.Op) : null;
        }


        private Asi parseOpApply(Asi op1)
        {
            var op = matched;
            var op2 = parseAsi();
            Contract.Assume(op2 != null);
            return new BinOpApply(new Ident(op, IdentType.Op), op1, op2);
        }


        private Boolean isDigit()
        {
            var ch = code[index];
            return ch >= '0' && ch <= '9';
        }


        private Boolean isLetter()
        {
            var ch = code[index];
            return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_';
        }


        private Boolean isOp()
        {
            var ch = code[index];
            return ch == '.' || ch == '+' || ch == '-' || ch == '*' ||
                   ch == '%' || ch == '=' || ch == ':' || ch == '!' ||
                   ch == '~' || ch == '@' || ch == '#' || ch == '^' ||
                   ch == '&' || ch == '/' || ch == '|' || ch == '<' ||
                   ch == '>' || ch == '?' || ch == ',' || ch == '$' ||
                   ch == ';' || ch == '`' || ch == '\\';
        }


        private void skipWhite()
        {
            matchUntil(isWhite);
        }


        private Boolean isWhite()
        {
            var ch = code[index];
            return ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
        }


        private Boolean matchUntil(Func<Boolean> isMatch)
        {
            var startIndex = index;
            while (index < code.Length && isMatch())
                index++;

            var length = index - startIndex;
            matched = code.Substring(startIndex, length);

            return length != 0;
        }
    }
}