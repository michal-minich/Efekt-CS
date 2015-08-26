﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Parser
    {
        String code;
        Int32 index;
        String matched = "";
        ValidationList validations;
        Int32 lineNumber;

        static readonly List<String> rightAssociativeOps = new List<String>
        { ":", "=", "*=", "/=", "%=", "+=", "-=", "<<=", ">>=", "&==", "^=", "|=" };

        static readonly Dictionary<String, Int32> opPrecedence
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

        Boolean wasNewLine;


        public AsiList Parse(String codeText, ValidationList validationList)
        {
            Contract.Requires(codeText != null);
            Contract.Ensures(Contract.Result<AsiList>() != null);

            code = codeText;
            index = 0;
            lineNumber = 1;
            validations = validationList;

            var items = new List<IAsi>();
            while (true)
            {
                var asi = parseCombinedAsi();
                if (asi == null)
                    break;
                wasNewLine = false;
                items.Add(asi);
            }

            return new AsiList(items) { Line = 1 };
        }


        [CanBeNull]
        IAsi parseCombinedAsi(String context = null)
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

                if (matchChar(',') || lookChar('@') || lookOp("=>")
                    || (context == "new" && lookChar('.')))
                    return asi;

                var ix = index;
                if (matchWhile(isOp) || matchSpecialOp())
                {
                    var op = matched;

                    var ident = asi as Ident;
                    if (op == "." && ident != null && ident.Name == "this")
                        validations.ThisMemberAccess(ident);

                    Int32 curOpPrecedence;
                    if (!opPrecedence.ContainsKey(op))
                    {
                        validations.GenericWarning(
                            "Operator '" + op + "' precedence is not defined.", asi);
                        curOpPrecedence = 0;
                    }
                    else
                    {
                        curOpPrecedence = opPrecedence[op];
                    }

                    if (rightAssociativeOps.Contains(op))
                    {
                        if (context == "right")
                        {
                            index = ix;
                            return asi;
                        }

                        var nextAsi = parseCombinedAsi("right");
                        Contract.Assume(nextAsi != null);
                        if (op == ":")
                        {
                            asi = a(new Declr((Ident)asi, nextAsi, null));
                        }
                        else
                        {
                            Contract.Assume(op != ".");
                            var i = a(new Ident(op, IdentCategory.Op));
                            asi = op == "="
                                ? (IAsi)new Assign((IExp)asi, (IExp)nextAsi)
                                : a(new BinOpApply(i, (IExp)asi, (IExp)nextAsi));
                        }
                    }
                    else if (curOpPrecedence <= prevOpPrecedence)
                    {
                        var op2 = parseAsi(op == ".");
                        Contract.Assume(op2 != null);
                        var i = a(new Ident(op, IdentCategory.Op));
                        var opa = a(new BinOpApply(i, (IExp)asi, (IExp)op2));
                        asi = opa;
                        if (op == ".")
                        {
                            var i1 = opa.Op2 as Ident;
                            if (i1 == null)
                            {
                                validations.ExpectedIdent(opa);
                                opa.Op2 = new Ident("__error", IdentCategory.Value);
                            }
                        }
                        prevOpPrecedence = curOpPrecedence;
                    }
                    else
                    {
                        var op3 = parseAsi(op == ".");
                        Contract.Assume(op3 != null);
                        var op1 = (BinOpApply)asi;
                        var opa4 = a(new BinOpApply(
                            a(new Ident(op, IdentCategory.Op)), op1.Op2, (IExp)op3));
                        asi = tryParseFnApply(opa4);
                        op1.Op2 = (IExp)asi;
                        asi = op1;
                    }

                    found = true;
                }
            } while (found);

            if (asi != null)
                asi = tryParseFnApply(asi);

            return asi;
        }


        IAsi parseAsi(Boolean skipFnApply = false)
        {
            skipWhite();
            var attrs = parseAttributes();
            var asi = tryParseInt() ?? tryParseBool() ?? tryParseVoid()
                      ?? tryParseFn() ?? tryParseVar() ?? tryParseNew()
                      ?? tryParseStruct() ?? tryParseIf() ?? tryParseImport()
                      ?? parseIterationKeywords() ?? parseExceptionRelated()
                      ?? parseChar() ?? parseString('"')
                      ?? tryParseAsiList() ?? tryParseArr() ?? tryParseBraced() ?? tryParseIdent();

            if (asi != null)
            {
                if (!skipFnApply)
                    asi = tryParseFnApply(asi);

                asi.Attributes = attrs;
            }
            else
            {
                if (attrs.Count != 0)
                    validations.GenericWarning("attributes are not followed by expression", attrs);
            }
            return asi;
        }


        List<IExp> parseAttributes()
        {
            var attrs = new List<IExp>();
            while (matchChar('@'))
            {
                var i = tryParseIdent();
                if (i == null)
                    validations.GenericWarning("Expected identifier after '@'", a(new Void()));
                var name = i == null ? "__attr" : i.Name;
                attrs.Add(a(new Ident("@" + name)));
                skipWhite();
            }
            return attrs;
        }


        Int tryParseInt() => matchWhile(isDigit) ? a(new Int(BigInteger.Parse(matched))) : null;


        Arr tryParseArr()
        {
            var arr = a(new Arr());
            var items = tryParseBracedList('[', ']', arr);
            if (items == null)
                return null;
            arr.Items = items.Select(toExp).ToList();
            return arr;
        }


        Fn tryParseFn()
        {
            if (!matchWord("fn"))
                return null;

            var fn = a(new Fn());

            skipWhite();
            var p = parseComaListUntil(fn, () => lookChar('{') || lookOp("=>"));

            var p2 = p.Select(e => expToDeclr(e, false, fn)).ToList();
            fn.Params = p2;

            skipWhite();
            if (matchOp("=>"))
            {
                var asi = parseCombinedAsi();
                if (asi == null)
                {
                    validations.GenericWarning("Expected expression or statement after fn ... =>",
                        fn);
                    asi = new Err();
                }
                fn.BodyItems = new List<IAsi> { asi };
            }
            else
            {
                var b = tryParseBracedList('{', '}', fn);
                if (b == null)
                {
                    validations.GenericWarning("expected '{...}' or '=>' after 'fn ...'", fn);
                    fn.BodyItems = new List<IAsi> { new Err() };
                }
                else
                {
                    fn.BodyItems = b;
                }
            }

            validateParamsOrder(fn);
            return fn;
        }


        void validateParamsOrder(Fn fn)
        {
            IAsi recentOptional = null;
            var n = 0;
            foreach (var p in fn.Params)
            {
                ++n;
                if (recentOptional == null)
                {
                    if (p.Value == null)
                        continue;
                    recentOptional = p;
                    fn.CountMandatoryParams = n - 1;
                }
                else
                {
                    if (p.Value == null)
                        validations.WrongParamsOrder(p, recentOptional);
                    recentOptional = p;
                }
            }
        }


        Struct tryParseStruct()
        {
            if (!matchWord("struct"))
                return null;
            var s = a(new Struct());
            skipWhite();
            var items = tryParseBracedList('{', '}', s);
            if (items == null)
            {
                validations.GenericWarning("missing open curly brace after 'struct'", s);
                items = new List<IAsi>();
            }
            foreach (var item in items)
            {
                var d = item as Declr;
                if (d == null)
                    validations.GenericWarning("Struct can contain only variables", item);
                else if (!d.IsVar)
                    validations.GenericWarning("Keyword 'var' is missing", item);
            }
            s.Items = items;
            return s;
        }


        Bool tryParseBool()
        {
            if (matchWord("true"))
                return a(new Bool(true));
            if (matchWord("false"))
                return a(new Bool(false));
            return null;
        }


        If tryParseIf()
        {
            if (!matchWord("if"))
                return null;
            var iff = a(new If());
            var t = parseCombinedAsi();

            var tIExp = t as IExp;
            if (t == null)
            {
                validations.NothingAfterIf(iff);
                iff.Test = a(new Err());
            }
            else if (tIExp == null)
            {
                validations.IfTestIsNotExp(t);
                iff.Test = a(new Err(t));
            }
            else
            {
                iff.Test = tIExp;
            }

            if (matchWord("then"))
            {
                iff.Then = parseCombinedAsi();
                if (iff.Then == null)
                {
                    validations.GenericWarning("expected expression after 'then'", iff);
                    iff.Then = a(new Err());
                }
                if (iff.Then is AsiList)
                    validations.GenericWarning("when block { } used 'then' should be omitted.", iff);
            }
            else if (lookChar('{'))
            {
                iff.Then = a(new AsiList(tryParseBracedList('{', '}', iff)));
            }
            else
            {
                validations.GenericWarning("expected 'then' or '{' after if", iff);
                iff.Then = a(new Err());
            }


            skipWhite();
            if (matchWord("else"))
            {
                iff.Otherwise = parseCombinedAsi();
                if (iff.Otherwise == null)
                {
                    validations.GenericWarning("expected expression after 'else'", iff);
                    iff.Otherwise = a(new Err());
                }
            }

            return iff;
        }


        AsiList tryParseAsiList()
        {
            var al = a(new AsiList());
            var b = tryParseBracedList('{', '}', al);
            if (b == null)
                return null;
            al.Items = b;
            return al;
        }


        Char parseChar()
        {
            var strParsed = parseString('\'');
            if (strParsed == null)
                return null;
            var str = strParsed as Arr;
            if (str == null)
                str = (Arr)((Err)strParsed).Item;
            Contract.Assume(str != null);
            Contract.Assume(str.Items != null);
            if (str.Items.Count == 0)
                throw new EfektException("char must have exactly one character, it has 0");
            if (str.Items.Count != 1)
                Console.WriteLine("char must have exactly one character, it has " +
                                  str.Items.Count);
            return (Char)str.Items.First();
        }


        IExp parseString(System.Char quote)
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
            var chars = new List<IExp>();
            for (var i = startAt; i < to; ++i)
                chars.Add(a(new Char(code[i])));
            var arr = a(new Arr(chars));
            return isUnterminated ? a(new Err(arr)) : (IExp)arr;
        }


        Import tryParseImport()
        {
            if (!matchWord("import"))
                return null;
            var imp = a(new Import());
            var asi = parseAsi();
            if (asi == null)
                validations.GenericWarning("Expected qualified identifier after import", imp);
            // todo validate if it is qualified ident (after member access op is here...) 
            imp.QualifiedIdent = (IExp)asi;
            return imp;
        }


        IAsi parseIterationKeywords()
        {
            if (matchWord("goto"))
                return new Goto(skipWhiteButNoNewLineAnd(tryParseIdent));

            if (matchWord("label"))
                return new Label(skipWhiteButNoNewLineAnd(tryParseIdent));

            if (matchWord("break if"))
                return new Break((IExp)skipWhiteButNoNewLineAnd(() => parseCombinedAsi()));

            if (matchWord("break"))
                return new Break(null);

            if (matchWord("continue if"))
                return new Continue((IExp)skipWhiteButNoNewLineAnd(() => parseCombinedAsi()));

            if (matchWord("continue"))
                return new Continue(null);

            if (matchWord("return"))
                return new Return(skipWhiteButNoNewLineAnd(() => parseCombinedAsi()));

            if (matchWord("repeat"))
            {
                var rp = new Repeat();
                skipWhite();
                var b = tryParseBracedList('{', '}', rp);
                if (b == null)
                {
                    validations.GenericWarning("Expected scoped after repeat", rp);
                    b = new List<IAsi>();
                }
                rp.Items = b;
                return rp;
            }

            if (matchWord("foreach"))
            {
                var fe = new ForEach();
                skipWhite();
                fe.Ident = tryParseIdent();
                Contract.Assume(fe.Ident != null);
                skipWhite();
                var isIn = matchWord("in");
                Contract.Assume(isIn);
                skipWhite();
                fe.Iterable = parseCombinedAsi();
                Contract.Assume(fe.Iterable != null);
                skipWhite();
                fe.Items = tryParseBracedList('{', '}', fe);
                Contract.Assume(fe.Items != null);
                return fe;
            }

            if (matchWord("assume"))
                return new Assume((IExp)parseCombinedAsi());

            if (matchWord("assert"))
                return new Assert((IExp)parseCombinedAsi());

            return null;
        }


        IAsi parseExceptionRelated()
        {
            if (matchWord("throw"))
            {
                skipWhite();
                IAsi ex = null;
                if (!wasNewLine)
                {
                    ex = parseCombinedAsi();
                }
                return new Throw((IExp)ex);
            }

            if (matchWord("try"))
            {
                var tr = a(new Try());


                skipWhite();
                tr.TryItems = tryParseBracedList('{', '}', tr);

                skipWhite();
                if (matchWord("catch"))
                {
                    skipWhite();
                    tr.ExVar = tryParseIdent();
                    skipWhite();
                    tr.CatchItems = tryParseBracedList('{', '}', tr);
                }

                skipWhite();
                if (matchWord("finally"))
                {
                    skipWhite();
                    tr.FinallyItems = tryParseBracedList('{', '}', tr);
                }

                return tr;
            }

            return null;
        }


        Void tryParseVoid() => matchWord("void") ? a(new Void()) : null;


        Ident tryParseIdent()
        {
            if (!matchWhile(isLetter))
                return null;

            if (matched == "op")
            {
                var isOpMatched = matchWhile(isOp);
                return isOpMatched ? a(new Ident(matched, IdentCategory.Op)) : new Ident("!!!");
            }

            var m1 = matched;
            matchWhile(isLetterOrDigit);
            return a(new Ident(m1 + matched));
        }


        IAsi tryParseBraced()
        {
            if (!matchChar('('))
                return null;

            var asi = parseCombinedAsi();

            if (!matchChar(')'))
                validations.GenericWarning("missing closing brace", asi ?? a(new Void()));

            return asi;
        }


        Declr tryParseVar() => !matchWord("var") ? null : tryParseDeclr(true);


        Declr tryParseDeclr(Boolean isVar = false)
        {
            skipWhite();
            var i = tryParseIdent();

            skipWhite();
            IExp t = null;
            if (matchText(":"))
            {
                skipWhite();
                t = tryParseIdent();
            }

            skipWhite();
            IExp v = null;
            if (matchText("="))
            {
                v = toExp(parseCombinedAsi());
            }

            return a(new Declr(i, t, v) { IsVar = isVar });
        }


        IExp toMandatoryExp(IAsi asi, IAsi owner)
        {
            var e = asi as IExp;
            if (e != null)
                return toExp(e);
            validations.GenericWarning("expression is required", owner);
            return new Err();
        }


        IExp toExp(IAsi asi)
        {
            var e = asi as IExp;
            if (e != null)
                return e;
            validations.GenericWarning("Expected expression instead of statement ", asi);
            return new Err(asi);
        }


        Declr expToDeclr([CanBeNull] IAsi asi, Boolean isVar, Fn owner)
        {
            if (asi == null)
            {
                validations.GenericWarning("expected ident, declaration or assignment", owner);
                return a(new Declr(new Ident("__error"), null, null));
            }

            var i = asi as Ident;
            if (i != null)
                return a(new Declr(i, null, null) { IsVar = isVar, Attributes = i.Attributes });

            var assign = asi as Assign;
            if (assign != null)
            {
                var i2 = assign.Target as Ident;
                if (i2 != null)
                {
                    return a(new Declr(i2, null, assign.Value)
                    {
                        IsVar = isVar,
                        Attributes = i2.Attributes
                    });
                }
                var d2 = assign.Target as Declr;
                if (d2 == null)
                    throw new EfektException("only identifier or declaration can be assigned");
                d2.IsVar = isVar;
                d2.Value = assign.Value;
                return d2;
            }

            var d = asi as Declr;
            if (d == null)
                throw new EfektException("declaration or identifier expected after var");
            d.IsVar = isVar;
            return d;
        }


        New tryParseNew()
        {
            if (!matchWord("new"))
                return null;
            var n = a(new New());
            var asi = parseCombinedAsi("new");
            var exp = asi as IExp;
            n.Exp = toMandatoryExp(exp, n);
            return n;
        }


        IAsi tryParseFnApply(IAsi asi)
        {
            skipWhite();
            if (wasNewLine)
                return asi;
            while (lookChar('('))
            {
                var fna = a(new FnApply(asi));
                var args = tryParseBracedList('(', ')', fna);
                Contract.Assert(args != null);
                fna.Args = args.Select(a => toMandatoryExp(a, fna)).ToList();
                asi = fna;
                skipWhite();
            }
            return asi;
        }


        List<IAsi> parseComaListUntil(IAsi owner, Func<Boolean> stopCheck)
        {
            var items = new List<IAsi>();
            skipWhite();
            while (true)
            {
                wasNewLine = false;
                if (!hasChars)
                {
                    validations.GenericWarning("source end reached and not stopped.", owner);
                    break;
                }
                if (stopCheck())
                    break;

                var asi = parseCombinedAsi();
                if (asi == null)
                    break;
                items.Add(asi);
                skipWhite();
            }
            return items;
        }


        List<IAsi> tryParseBracedList(System.Char startBrace, System.Char endBrace, IAsi owner)
        {
            if (!matchChar(startBrace))
                return null;

            return parseComaListUntil(owner, () => matchChar(endBrace));
        }


        static Boolean isDigit(System.Char ch) => ch >= '0' && ch <= '9';


        static Boolean isLetterOrDigit(System.Char ch) => isLetter(ch) || isDigit(ch);


        static Boolean isLetter(System.Char ch)
            => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_';


        static Boolean isOp(System.Char ch)
            => ch == '.' || ch == '+' || ch == '-' || ch == '*' ||
               ch == '%' || ch == '=' || ch == ':' || ch == '!' ||
               ch == '~' || ch == '@' || ch == '#' || ch == '^' ||
               ch == '&' || ch == '/' || ch == '|' || ch == '<' ||
               ch == '>' || ch == '?' || ch == ',' || ch == '$' ||
               ch == ';' || ch == '`' || ch == '\\';


        T skipWhiteAnd<T>(Func<T> parseFn)
        {
            skipWhite();
            return parseFn();
        }


        T skipWhiteButNoNewLineAnd<T>(Func<T> parseFn) where T : class, IAsi
        {
            skipWhite();
            if (wasNewLine)
                return null;
            return parseFn();
        }


        void skipWhite()
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


        void skipBlanks()
        {
            var res = matchWhile(isWhite);
            if (res)
                wasNewLine = wasNewLine || matched.Any(isNewLine);
        }


        void skipCommentLine()
        {
            if (!matchText("--"))
                return;
            matchWhile(ch => !isNewLine(ch));
        }


        void skipCommentMulti()
        {
            if (!matchText("/*"))
                return;
            while (index < code.Length - 1 && !(code[index] == '*' && code[index + 1] == '/'))
            {
                if (code[index] == '\n')
                    ++lineNumber;
                ++index;
            }
            ++index;
            ++index;
        }


        Boolean isWhite(System.Char ch)
        {
            if (ch == '\n')
                ++lineNumber;
            return ch == ' ' || ch == '\t' || isNewLine(ch);
        }


        static Boolean isNewLine(System.Char ch) => ch == '\r' || ch == '\n';


        Boolean matchChar(System.Char ch)
        {
            if (!lookChar(ch))
                return false;
            ++index;
            return true;
        }


        Boolean lookChar(System.Char ch) => index < code.Length && code[index] == ch;


        Boolean matchText(String text)
        {
            var isMatch = index + text.Length <= code.Length
                          && code.IndexOf(text, index) == index;
            if (isMatch)
                index += text.Length;
            return isMatch;
        }


        Boolean lookOp(String w)
        {
            // todo
            return lookWord(w);
        }


        Boolean matchOp(String w)
        {
            // todo
            return matchWord(w);
        }


        Boolean lookWord(String w)
        {
            Contract.Ensures(Contract.OldValue(index) == index);

            if (index + w.Length > code.Length)
                return false;
            if (code.Substring(index, w.Length) != w)
                return false;
            if (index + w.Length < code.Length)
            {
                if (isLetter(code[index + w.Length]))
                    return false;
            }
            return true;
        }


        Boolean matchWord(String w)
        {
            Contract.Ensures(Contract.Result<Boolean>() == (Contract.OldValue(index) < index));
            Contract.Ensures(Contract.Result<Boolean>() != (Contract.OldValue(index) == index));

            if (!lookWord(w))
                return false;
            matched = code.Substring(index, w.Length);
            index += w.Length;
            return true;
        }


        Boolean matchWhile(Func<System.Char, Boolean> isMatch)
        {
            Contract.Requires(index >= 0);
            Contract.Requires(isMatch != null);
            Contract.Ensures(Contract.Result<Boolean>() == matched.Length > 0);


            if (index > code.Length)
                return false;

            var startIndex = index;
            while (index < code.Length && isMatch(code[index]))
                index++;

            var length = index - startIndex;
            if (length == 0)
            {
                matched = "";
                return false;
            }
            matched = code.Substring(startIndex, length);
            return true;
        }


        Boolean matchSpecialOp() => matchWord("and") || matchWord("or");


        Boolean hasChars => index < code.Length;


        T a<T>(T asi) where T : Asi
        {
            Contract.Requires(asi != null);
            Contract.Ensures(Contract.Result<T>() == asi);

            asi.Line = lineNumber;
            return asi;
        }
    }
}