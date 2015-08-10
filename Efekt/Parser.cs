using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
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

        private Boolean wasNewLine;


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
                wasNewLine = false;
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
                    return asi;

                var ix = index;
                if (matchUntil(isOp) || matchSpecialOp())
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
                            asi = new Declr((Ident) asi, type);
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

            if (asi != null)
                asi = parseFnApply(asi);

            return asi;
        }


        private Asi parseAsi()
        {
            skipWhite();
            var asi = parseInt() ?? parseArr() ?? parseFn() ?? parseVar() ?? parseNew()
                      ?? parseStruct() ?? parseBool() ?? parseIf() ?? parseAsiList()
                      ?? parseChar() ?? parseString('"') ?? parseImport()
                      ?? parseVoid() ?? parseIdent() ?? parseBraced();
            //Contract.Assume((asi == null) == (index > code.Length || String.IsNullOrWhiteSpace(code)));
            if (asi != null)
                asi = parseFnApply(asi);
            return asi;
        }


        private Int parseInt() => matchUntil(isDigit) ? new Int(matched) : null;


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
                throw new EfektException("expected '(...)' after 'fn'");

            p = p.Select(e => expToDeclrOrAssign(e, false)).ToList();

            skipWhite();
            var b = parseBracedList('{', '}');

            if (b == null)
                throw new EfektException("expected '{...}' after 'fn (...)'");

            return new Fn(p, b);
        }


        private Struct parseStruct()
        {
            if (!matchWord("struct"))
                return null;

            skipWhite();

            var items = parseBracedList('{', '}');
            if (items == null)
                throw new EfektException("missing open curly brace after 'struct'");

            return new Struct(items);
        }


        private Bool parseBool()
        {
            if (matchWord("true"))
                return new Bool(true);
            if (matchWord("false"))
                return new Bool(false);
            return null;
        }


        private If parseIf()
        {
            if (!matchWord("if"))
                return null;
            var t = parseCombinedAsi();

            if (t == null)
                throw new EfektException("expected expression after then");

            Asi then;
            if (matchWord("then"))
            {
                then = parseCombinedAsi();
                if (then == null)
                    throw new EfektException("expected expression after then");
            }
            else
                throw new EfektException("expected then after if");


            Asi o = null;
            if (matchWord("else"))
            {
                o = parseCombinedAsi();
                if (o == null)
                    throw new EfektException("expected expression after else");
            }

            return new If(t, then, o);
        }


        private Asi parseAsiList()
        {
            var b = parseBracedList('{', '}');
            return b == null ? null : new AsiList(b);
        }


        private Char parseChar()
        {
            var str = parseString('\'');
            if (str == null)
                return null;
            if (!str.Items.Any())
                throw new EfektException("char must have exactly one character, it has 0");
            if (str.Items.Count() != 1)
                Console.WriteLine("char must have exactly one character, it has " +
                                  str.Items.Count());
            return (Char) str.Items.First();
        }


        private Arr parseString(System.Char quote)
        {
            if (!matchChar(quote))
                return null;
            var startAt = index;
            var firstNewLineAt = 0;
            var isUnterminated = false;
            while (true)
            {
                if (index >= code.Length)
                {
                    isUnterminated = true;
                    /*throw new EfektException*/
                    Console.WriteLine("Unterminated string constant " +
                                      "at the end of the file.");

                    break;
                }
                var ch = code[index];
                ++index;
                if (ch == quote)
                    break;
                if (firstNewLineAt == 0 && isNewLine(ch))
                    firstNewLineAt = index - 1;
            }
            Int32 to;
            if (isUnterminated && firstNewLineAt != 0)
            {
                to = firstNewLineAt;
                index = firstNewLineAt;
            }
            else
            {
                to = isUnterminated ? index : index - 1;
                if (to > code.Length)
                    to = code.Length;
            }
            var chars = new List<Char>();
            for (var i = startAt; i < to; ++i)
                chars.Add(new Char(code[i]));
            var arr = new Arr(chars);
            return isUnterminated ? /*new Err(arr)*/arr : arr;
        }


        private Import parseImport()
        {
            if (!matchWord("import"))
                return null;
            var asi = parseAsi();
            return new Import(asi);
        }


        private Void parseVoid() => matchWord("void") ? new Void() : null;


        private Ident parseIdent()
        {
            var isLetterMatched = matchUntil(isLetter);
            if (!isLetterMatched)
                return null;

            if (matched != "op")
            {
                var m1 = matched;
                matchUntil(isLetterOrDigit);
                var name = m1 + matched;
                return new Ident(
                    name,
                    System.Char.IsUpper(name[0]) ? IdentType.Type : IdentType.Value);
            }

            var isOpMatched = matchUntil(isOp);
            return isOpMatched ? new Ident(matched, IdentType.Op) : null;
        }


        private Asi parseBraced()
        {
            if (!matchChar('('))
                return null;

            var asi = parseCombinedAsi();

            if (!matchChar(')'))
                throw new EfektException("missing closing brace");

            return asi;
        }


        private Asi parseVar()
        {
            if (!matchWord("var"))
                return null;
            skipWhite();
            var asi = parseCombinedAsi();

            return expToDeclrOrAssign(asi, true);
        }


        private static Asi expToDeclrOrAssign([CanBeNull] Asi asi, Boolean isVar)
        {
            if (asi == null)
                throw new EfektException("expected ident, declaration or assignment");

            var i = asi as Ident;
            if (i != null)
                return new Declr(i, null) {IsVar = isVar};

            var o = asi as BinOpApply;
            if (o != null)
            {
                if (o.Op.Name != "=")
                    throw new EfektException("only assignment can be variable");
                var i2 = o.Op1 as Ident;
                if (i2 != null)
                {
                    o.Op1 = new Declr(i2, null) {IsVar = isVar};
                    return o;
                }
                var d2 = o.Op1 as Declr;
                if (d2 == null)
                    throw new EfektException("only identifier or declaration can be assigned");
                d2.IsVar = isVar;
                return o;
            }

            var d = asi as Declr;
            if (d == null)
                throw new EfektException("declaration or identifier expected after var");
            d.IsVar = isVar;
            return d;
        }


        internal static Ident GetIdentFromDeclrLikeAsi(Asi a)
        {
            var i = a as Ident;
            if (i != null)
                return i;
            var d = a as Declr;
            if (d != null)
                return d.Ident;
            var o = a as BinOpApply;
            if (o != null)
            {
                if (o.Op.Name == "=")
                {
                    var i2 = o.Op1 as Ident;
                    if (i2 != null)
                        return i2;
                    var d2 = o.Op1 as Declr;
                    if (d2 != null)
                        return d2.Ident;
                    throw new EfektException("expression of type " + a.GetType().Name +
                                             " cannot be assigned");
                }
                throw new EfektException("expected assignment operator");
            }
            throw new EfektException("expression of type " + a.GetType().Name + "is unexpected");
        }


        private New parseNew()
        {
            if (!matchWord("new"))
                return null;
            skipWhite();
            var asi = parseCombinedAsi();
            if (asi == null)
                throw new EfektException("expression required after new");
            return new New(asi);
        }


        private Asi parseFnApply(Asi asi)
        {
            skipWhite();
            if (wasNewLine)
                return asi;
            while (matchChar('('))
            {
                --index;
                var args = parseBracedList('(', ')');
                asi = new FnApply(asi, args);
                skipWhite();
            }
            return asi;
        }


        private List<Asi> parseBracedList(System.Char startBrace, System.Char endBrace)
        {
            if (!matchChar(startBrace))
                return null;

            var items = new List<Asi>();
            skipWhite();
            while (!matchChar(endBrace))
            {
                if (!hasChars)
                    throw new EfektException("missing closing brace " + endBrace +
                                             " and source end reached");

                wasNewLine = false;
                var asi = parseCombinedAsi();
                items.Add(asi);
                skipWhite();
            }
            return items;
        }


        private Boolean isDigit()
        {
            if (index >= code.Length)
                return false;
            var ch = code[index];
            return ch >= '0' && ch <= '9';
        }


        private Boolean isLetter() => index < code.Length && isLetter(code[index]);


        private Boolean isLetterOrDigit() => isLetter() || isDigit();


        private static Boolean isLetter(System.Char ch)
            => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_';


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
            var wasAnyNewLine = false;
            Int32 prevIndex;
            do
            {
                prevIndex = index;
                skipBlanks();
                if (wasNewLine)
                    wasAnyNewLine = true;
                skipCommentLine();
                skipCommentMulti();
            } while (prevIndex != index);
            wasNewLine = wasAnyNewLine;
        }


        private void skipBlanks()
        {
            var res = matchUntil(isWhite);
            if (res)
                wasNewLine = wasNewLine || matched.Any(isNewLine);
        }


        private void skipCommentLine()
        {
            if (!matchText("--"))
                return;
            while (index < code.Length && !isNewLine(code[index]))
                ++index;
            ++index;
        }


        private void skipCommentMulti()
        {
            if (!matchText("/*"))
                return;
            while (index < code.Length - 1 && !(code[index] == '*' && code[index + 1] == '/'))
                ++index;
            ++index;
            ++index;
        }


        private Boolean isWhite()
        {
            if (index >= code.Length)
                return false;
            var ch = code[index];
            return ch == ' ' || ch == '\t' || isNewLine(ch);
        }


        private static Boolean isNewLine(System.Char ch) => ch == '\r' || ch == '\n';


        private Boolean matchChar(System.Char ch)
        {
            if (index >= code.Length || code[index] != ch)
                return false;

            ++index;
            return true;
        }


        private Boolean matchText(String text)
        {
            var isMatch = index + text.Length <= code.Length
                          && code.IndexOf(text, index) == index;
            if (isMatch)
                index += text.Length;
            return isMatch;
        }


        private Boolean matchWord(String w)
        {
            Contract.Ensures(Contract.Result<Boolean>() == (Contract.OldValue(index) < index));
            Contract.Ensures(Contract.Result<Boolean>() != (Contract.OldValue(index) == index));

            if (index + w.Length > code.Length)
                return false;
            if (code.Substring(index, w.Length) != w)
                return false;
            if (index + w.Length < code.Length)
            {
                if (isLetter(code[index + w.Length]))
                    return false;
            }
            matched = code.Substring(index, w.Length);
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


        private Boolean matchSpecialOp() => matchWord("and") || matchWord("or");


        private Boolean hasChars => index < code.Length;
    }
}