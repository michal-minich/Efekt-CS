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

            var items = new List<Asi>();

            while (true)
            {
                skipWhite();
                var asi = parseInt();
                if (asi == null)
                    break;

                items.Add(asi);
            }

            return new AsiList(items);
        }


        private Int parseInt()
        {
            return matchUntil(isDigit) ? new Int(matched) : null;
        }


        private Boolean isDigit()
        {
            var ch = code[index];
            return ch >= '0' && ch <= '9';
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