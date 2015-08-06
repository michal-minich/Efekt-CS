using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Parser
    {
        private String code;
        private Int32 index;
        private String matched = "";

        private static readonly List<String> rightAssociativeOps = new List<String>
        {":", "=", "*=", "/=", "%=", "+=", "-=", "<<=", ">>=", "&==", "^=", "|="};

        private static readonly Dictionary<String, Int32> opPrecedence = new Dictionary<String, Int32>
        {
            ["."] = 160,
            [":"] = 140,
            ["*"] = 130,
            ["/"] = 130,
            ["%"] = 130,
            ["+"] = 120,
            ["-"] = 120,
            ["<<"] = 110,
            [">>"] = 110,
            ["<"] = 100,
            [">"] = 100,
            [">="] = 100,
            ["<="] = 100,
            ["=="] = 60,
            ["!="] = 60,
            ["&"] = 50,
            ["^"] = 40,
            ["|"] = 30,
            ["$"] = 30,
            ["&&"] = 20,
            ["and"] = 20,
            ["||"] = 10,
            ["or"] = 10,
            ["="] = 3,
            ["*="] = 3,
            ["/="] = 3,
            ["%="] = 3,
            ["+="] = 3,
            ["-="] = 3,
            ["<<="] = 3,
            [">>="] = 3,
            ["&=="] = 3,
            ["^="] = 3,
            ["|="] = 3,
            ["] ="] = 2
        };


        public AsiList Parse(String codeText)
        {
            Contract.Requires(codeText != null);
            Contract.Ensures(Contract.Result<AsiList>() != null);

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


        [CanBeNull]
        private Asi parseCombinedAsi(Boolean isRight = false)
        {
            var asi = parseAsi();
            if (asi == null)
                return null;
            Boolean found;
            var prevOpPrecedence = Int32.MaxValue;
            do
            {
                found = false;
                skipWhite();

                if (matchChar(','))
                {
                    ++index;
                    return asi;
                }

                var ix = index;
                if (matchUntil(isOp))
                {
                    var op = matched;
                    var curOpPrecedence = opPrecedence[op];
                    if (rightAssociativeOps.Contains(op))
                    {
                        if (isRight)
                        {
                            index = ix;
                            return asi;
                        }

                        if (op == ":")
                        {
                            var type = parseCombinedAsi(true);
                            Contract.Assume(type != null);
                            asi = new Declr((Ident) asi, type, null);
                        }
                        else
                        {
                            var op2 = parseCombinedAsi(true);
                            Contract.Assume(op2 != null);
                            asi = new BinOpApply(new Ident(op, IdentType.Op), asi, op2);
                        }
                    }
                    else if (curOpPrecedence <= prevOpPrecedence)
                    {
                        var op2 = parseAsi();
                        Contract.Assume(op2 != null);
                        asi = new BinOpApply(new Ident(op, IdentType.Op), asi, op2);
                        prevOpPrecedence = curOpPrecedence;
                    }
                    else
                    {
                        var op2 = parseAsi();
                        Contract.Assume(op2 != null);
                        var op1 = (BinOpApply) asi;
                        op1.Op2 = new BinOpApply(new Ident(op, IdentType.Op), op1.Op2, op2);
                    }

                    found = true;
                }
            } while (found);
            return asi;
        }


        private Asi parseAsi()
        {
            skipWhite();
            var asi = parseInt() ?? parseIdent() ?? parseArr() ?? parseBraced();
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


        private Arr parseArr()
        {
            if (!matchChar('['))
                return null;

            var items = new List<Asi>();
            skipWhite();
            while (!matchChar(']'))
            {
                var asi = parseCombinedAsi();
                items.Add(asi);
                skipWhite();
            }

            return new Arr(items);
        }


        private Asi parseBraced()
        {
            if (!matchChar('('))
                return null;

            var asi = parseCombinedAsi();

            if (!matchChar(')'))
                throw new Exception("missing closing brace");

            return asi;
        }


        private Boolean isDigit()
        {
            if (index >= code.Length)
                return false;
            var ch = code[index];
            return ch >= '0' && ch <= '9';
        }


        private Boolean isLetter()
        {
            if (index >= code.Length)
                return false;
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
            if (index >= code.Length)
                return false;
            var ch = code[index];
            return ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
        }


        private Boolean matchChar(Char ch)
        {
            if (index < code.Length && code[index] == ch)
            {
                ++index;
                return true;
            }
            return false;
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