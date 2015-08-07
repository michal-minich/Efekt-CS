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

        private static readonly Dictionary<String, Int32> opPrecedence
            = new Dictionary<String, Int32>
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
            var asi = parseInt() ?? parseArr() ?? parseFn() ?? parseStruct() ?? parseIdent()
                      ?? parseBraced();

            asi = parseFnApply(asi);

            return asi;
        }


        private Int parseInt()
        {
            return matchUntil(isDigit) ? new Int(matched) : null;
        }


        private Arr parseArr()
        {
            var items = parseBracedList('[', ']');
            return items == null ? null : new Arr(items);
        }


        private Fn parseFn()
        {
            if (!matchWord("fn"))
                return null;

            skipWhite();
            var p = parseBracedList('(', ')');

            if (p == null)
                throw new Exception("expected '(...)' after 'fn'");

            skipWhite();
            var b = parseBracedList('{', '}');

            if (b == null)
                throw new Exception("expected '{...}' after 'fn (...)'");

            return new Fn(p, b);
        }


        private Struct parseStruct()
        {
            if (!matchWord("struct"))
                return null;

            skipWhite();

            var items = parseBracedList('{', '}');
            if (items == null)
                throw new Exception("missing open curly brace after 'struct'");

            return new Struct(items);
        }


        private Ident parseIdent()
        {
            var isLetterMatched = matchUntil(isLetter);
            if (!isLetterMatched)
                return null;

            if (matched != "op")
                return new Ident(
                    matched,
                    Char.IsUpper(matched[0]) ? IdentType.Type : IdentType.Value);

            var isOpMatched = matchUntil(isOp);
            return isOpMatched ? new Ident(matched, IdentType.Op) : null;
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


        private List<Asi> parseBracedList(Char startBrace, Char endBrace)
        {
            if (!matchChar(startBrace))
                return null;

            var items = new List<Asi>();
            skipWhite();
            while (!matchChar(endBrace))
            {
                if (!hasChars)
                    throw new Exception("missing closing brace " + endBrace +
                                        " and source end reached");
                var asi = parseCombinedAsi();
                items.Add(asi);
                skipWhite();
            }
            return items;
        }


        private Asi parseFnApply(Asi asi)
        {
            skipWhite();
            while (matchChar('('))
            {
                --index;
                var args = parseBracedList('(', ')');
                asi = new FnApply(asi, args);
                skipWhite();
            }
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
            if (index >= code.Length || code[index] != ch)
                return false;

            ++index;
            return true;
        }


        private Boolean matchWord(String w)
        {
            if (index + w.Length >= code.Length)
                return false;

            if (code.Substring(index, w.Length) != w)
                return false;

            index += w.Length;
            return true;
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


        private Boolean hasChars => index < code.Length;
    }
}