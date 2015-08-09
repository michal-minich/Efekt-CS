﻿using System;
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


            return asi;
        }


        private Asi parseAsi()
        {
            skipWhite();
            var asi = parseInt() ?? parseArr() ?? parseFn() ?? parseVar() ?? parseNew()
                      ?? parseStruct() ?? parseBool()
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


        private Void parseVoid() => matchWord("void") ? new Void() : null;


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
            var ident = parseIdent();
            if (ident == null)
                throw new EfektException("ident required after new");
            return new New(ident);
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


        private List<Asi> parseBracedList(Char startBrace, Char endBrace)
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


        private static Boolean isLetter(Char ch)
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


        private Boolean skipWhite()
        {
            wasNewLine = false;
            var res = matchUntil(isWhite);
            if (res)
                wasNewLine = matched.Any(isNewLine);
            return res;
        }


        private Boolean isWhite()
        {
            if (index >= code.Length)
                return false;
            var ch = code[index];
            return ch == ' ' || ch == '\t' || isNewLine(ch);
        }


        private static Boolean isNewLine(Char ch) => ch == '\r' || ch == '\n';


        private Boolean matchChar(Char ch)
        {
            if (index >= code.Length || code[index] != ch)
                return false;

            ++index;
            return true;
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