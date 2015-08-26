using System;
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

                if (matchChar(','))
                    return asi;

                var ix = index;
                if (matchWhile(isOp) || matchSpecialOp())
                {
                    var op = matched;
                    if (op == "=>")
                    {
                        index -= 2;
                        return asi;
                    }
                    if (op == "@")
                    {
                        index -= 1;
                        return asi;
                    }

                    if (context == "new" && op == ".")
                    {
                        index = ix;
                        return asi;
                    }
                    var ident = asi as Ident;
                    if (op == "." && ident != null && ident.Name == "this")
                        validations.ThisMemberAccess(ident);

                    if (!opPrecedence.ContainsKey(op))
                        throw new EfektException(
                            "Operator '" + op + "' precedence is not defined.");
                    var curOpPrecedence = opPrecedence[op];
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
                        IAsi op3 = null;
                        op3 = parseAsi(op == ".");
                        Contract.Assume(op3 != null);
                        var op1 = (BinOpApply)asi;
                        var opa4 = a(new BinOpApply(
                            a(new Ident(op, IdentCategory.Op)), op1.Op2, (IExp)op3));
                        asi = parseFnApply(opa4);
                        op1.Op2 = (IExp)asi;
                        asi = op1;
                    }

                    found = true;
                }
            } while (found);

            if (asi != null)
                asi = parseFnApply(asi);

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
                    asi = parseFnApply(asi);

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
                    validations.GenericWarning("Expected identifier after '@'");
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
            var items = tryParseBracedList('[', ']');
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
            var p = parseComaListUntil('{');
            --index;

            var p2 = p.Select(e => expToDeclrOrAssign(e, false)).ToList();
            fn.Params = p2;

            skipWhite();
            if (matchWord("=>"))
            {
                var asi = parseCombinedAsi();
                if (asi == null)
                {
                    validations.GenericWarning("Expected expression or statement after fn ... =>");
                    asi = new Err();
                }
                fn.BodyItems = new List<IAsi> { asi };
            }
            else
            {
                var b = tryParseBracedList('{', '}');
                if (b == null)
                {
                    validations.GenericWarning("expected '{...}' or '=>' after 'fn ...'");
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
            var items = tryParseBracedList('{', '}');
            if (items == null)
            {
                validations.GenericWarning("missing open curly brace after 'struct'");
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
                    validations.GenericWarning("expected expression after 'then'");
                    iff.Then = a(new Err());
                }
                if (iff.Then is AsiList)
                    validations.GenericWarning("when block { } used 'then' should be omitted.");
            }
            else if (lookChar('{'))
            {
                iff.Then = a(new AsiList(tryParseBracedList('{', '}')));
            }
            else
            {
                validations.GenericWarning("expected 'then' or '{' after if");
                iff.Then = a(new Err());
            }


            skipWhite();
            if (matchWord("else"))
            {
                iff.Otherwise = parseCombinedAsi();
                if (iff.Otherwise == null)
                {
                    validations.GenericWarning("expected expression after 'else'");
                    iff.Otherwise = a(new Err());
                }
            }

            return iff;
        }


        AsiList tryParseAsiList()
        {
            var al = a(new AsiList());
            var b = tryParseBracedList('{', '}');
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
                validations.GenericWarning("Expected qualified identifier after import");
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
                return new Repeat(skipWhiteAnd(() => tryParseBracedList('{', '}')));

            if (matchWord("foreach"))
            {
                skipWhite();
                var ident = tryParseIdent();
                Contract.Assume(ident != null);
                skipWhite();
                var isIn = matchWord("in");
                Contract.Assume(isIn);
                skipWhite();
                var iterable = parseCombinedAsi();
                Contract.Assume(iterable != null);
                skipWhite();
                var items = tryParseBracedList('{', '}');
                Contract.Assume(items != null);
                var fe = new ForEach(ident, iterable, items);
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
                List<IAsi> c = null;
                List<IAsi> f = null;
                Ident exVar = null;

                skipWhite();
                var t = tryParseBracedList('{', '}');

                skipWhite();
                if (matchWord("catch"))
                {
                    skipWhite();
                    exVar = tryParseIdent();
                    skipWhite();
                    c = tryParseBracedList('{', '}');
                }

                skipWhite();
                if (matchWord("finally"))
                {
                    skipWhite();
                    f = tryParseBracedList('{', '}');
                }

                return new Try(t, c, exVar, f);
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
                validations.GenericWarning("missing closing brace");

            return asi;
        }


        Exp tryParseVar() => !matchWord("var") ? null : tryParseDeclr(true);


        Exp tryParseDeclr(Boolean isVar = false)
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


        Declr expToDeclrOrAssign([CanBeNull] IAsi asi, Boolean isVar)
        {
            if (asi == null)
                throw new EfektException("expected ident, declaration or assignment");

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


        IAsi parseFnApply(IAsi asi)
        {
            skipWhite();
            if (wasNewLine)
                return asi;
            while (lookChar('('))
            {
                var args = tryParseBracedList('(', ')');
                Contract.Assert(args != null);
                var fna = a(new FnApply(asi));
                fna.Args = args.Select(a => toMandatoryExp(a, fna)).ToList();
                asi = fna;
                skipWhite();
            }
            return asi;
        }


        List<IAsi> parseComaListUntil(System.Char stopChar)
        {
            var items = new List<IAsi>();
            skipWhite();
            while (!matchChar(stopChar))
            {
                if (!hasChars)
                    throw new EfektException("missing closing brace " + stopChar +
                                             " and source end reached");

                wasNewLine = false;
                var asi = parseCombinedAsi();
                if (asi == null)
                    return items;
                items.Add(asi);
                skipWhite();
                if (matchWord("=>"))
                {
                    index -= 1;
                    break;
                }
            }
            return items;
        }


        List<IAsi> tryParseBracedList(System.Char startBrace, System.Char endBrace)
        {
            if (!matchChar(startBrace))
                return null;

            return parseComaListUntil(endBrace);
        }


        Boolean isDigit()
        {
            if (index >= code.Length)
                return false;
            var ch = code[index];
            return ch >= '0' && ch <= '9';
        }


        Boolean isLetter() => index < code.Length && isLetter(code[index]);


        Boolean isLetterOrDigit() => isLetter() || isDigit();


        static Boolean isLetter(System.Char ch)
            => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_';


        Boolean isOp()
        {
            var ch = code[index];
            return ch == '.' || ch == '+' || ch == '-' || ch == '*' ||
                   ch == '%' || ch == '=' || ch == ':' || ch == '!' ||
                   ch == '~' || ch == '@' || ch == '#' || ch == '^' ||
                   ch == '&' || ch == '/' || ch == '|' || ch == '<' ||
                   ch == '>' || ch == '?' || ch == ',' || ch == '$' ||
                   ch == ';' || ch == '`' || ch == '\\';
        }


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
            var startIndex = index;
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
            while (index < code.Length && !isNewLine(code[index]))
                ++index;
            ++lineNumber;
            ++index;
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


        Boolean isWhite()
        {
            if (index >= code.Length)
                return false;
            var ch = code[index];
            if (ch == '\n')
            {
                ++lineNumber;
            }
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


        Boolean matchWhile(Func<Boolean> isMatch)
        {
            Contract.Requires(isMatch != null);
            Contract.Ensures(Contract.Result<Boolean>() == matched.Length > 0);
            Contract.Assume(index >= 0);
            if (index > code.Length)
                return false;

            var startIndex = index;
            while (index < code.Length && isMatch())
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