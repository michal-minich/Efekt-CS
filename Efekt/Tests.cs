﻿using System;


namespace Efekt
{
    public static class Tests
    {
        public static void Test()
        {
            testParser();
            testInterpreter();
            Console.WriteLine("All Tests OK");
        }


        private static void testParser()
        {
            parse("");
            parse(" ", "");

            parse("void");

            parse("123");
            parse(" 123  456 ", "123\n456");

            parse("a");
            parse(" abc de ", "abc\nde");

            parse("op+");
            parse(" op-*- op== ", "op-*-\nop==");

            parse("1+2", "1 + 2");
            parse(" a+ b -cd * efg ", "a + b - cd * efg");

            parse("a : Int");
            parse("a : Bool | Int");

            parseWithBraces("1 + 2 * 3", "(1 + (2 * 3))");
            parseWithBraces("a * b + c", "((a * b) + c)");
            parseWithBraces("1 + 2 * 3 + 4 * 5", "((1 + (2 * 3)) + (4 * 5))");
            parseWithBraces("a * b + c * d + e", "(((a * b) + (c * d)) + e)");
            parseWithBraces("3 * 4 + 5 + 6", "(((3 * 4) + 5) + 6)");
            parseWithBraces("c + d * e * f", "(c + ((d * e) * f))");
            parseWithBraces("1 + 2 + 3 * 4 + 5 + 6 * 7", "((((1 + 2) + (3 * 4)) + 5) + (6 * 7))");
            parseWithBraces("a * b * c + d * e * f + g", "((((a * b) * c) + ((d * e) * f)) + g)");

            parse("a = 1");
            parse("a = 1 + 2");
            parse("a : Int = 1");
            parseWithBraces("a $ b = 1 + 2", "((a $ b) = (1 + 2))");
            parseWithBraces("a : Int = 1 + 2", "(a : Int = (1 + 2))");
            parseWithBraces("a : Bool | Int = 1 + 2", "(a : (Bool | Int) = (1 + 2))");

            parse("[]");
            parse("[] + []");
            parse("[[]]");
            parse("[[] + [[]]]");

            parse("[1]");
            parse("[1, 2, 3]");
            parseWithBraces("[1, 2, 3] + [4 + 5]", "([1, 2, 3] + [(4 + 5)])");
            parseWithBraces("[1 + 2, 3 * 4, 5]", "[(1 + 2), (3 * 4), 5]");
            parseWithBraces("[1, 2, [3, 4 + 5], 6]", "[1, 2, [3, (4 + 5)], 6]");

            parse("(1)", "1");
            parse("(1 + 2)", "1 + 2");
            parse("(1) + (2)", "1 + 2");
            parseWithBraces("(1 + 2) * 3", "((1 + 2) * 3)");
            parseWithBraces("1 + (2 * 3)", "(1 + (2 * 3))");
            parseWithBraces("(1 + (2) * 3)", "(1 + (2 * 3))");

            parse("struct { }");
            parse("struct { a }");
            parse("struct { a b }", "struct { a\nb }");
            parseWithBraces("struct { a b + c }", "struct { a\n(b + c) }");

            parse("fn () { }");
            parse("fn (a) { }");
            parse("fn () { 1 }");
            parse("fn (a) { 1 }");
            parse("fn (a) { struct { } }");
            parse("fn (a = 1) { }");
            parse("fn (a, b) { }");
            parseWithBraces("fn (a, b = 2 + 3, c = 4) { }", "fn (a, (b = (2 + 3)), (c = 4)) { }");
            parse("fn () { fn (a) { b } }");
            parse("fn (a = fn (b) { c }) { d }");

            parse("a()");
            parse("(a)()", "a()");
            parse("(a)(b)", "a(b)");
            parseWithBraces("(a + b)()", "(a + b)()");
            parseWithBraces("a + b()", "(a + b())");
            parseWithBraces("a + b() + c()", "((a + b()) + c())");
            parseWithBraces("a() + b() * c()", "(a() + (b() * c()))");
            parse("fn () { }()");
            parse("a(fn () { })");
            parse("fn () { }(fn () { })");
            parse("(a)()()", "a()()");
            parse("(a)(b())", "a(b())");
            parse("a(b()())()");

            parse("new A");
            parse("a = new A");
            parse("a = new A()");

            parse("var a");
            parse("var a = 1");
            parse("var a = 1 + 2");
            parse("var a : T");
            parse("var a : A | B");
            parse("var a : T = 1");
            parse("var a : T = 1 + 2");
            parse("var a : A | B = 1 + 2");

            parse("a1");
            parse("_a1");
            parse("a_1");
            parse("a1_");
            parse("_a1_");
            parse("_a_1");
            parse("_a1_");
            parse("_a_1_");
            parse("a_1_");
            parse("a_1b");
            parse("a_1_b");
        }


        private static void testInterpreter()
        {
            eval("", "void");
            eval("1");
            eval("var a = 1", "1");
            eval("var a : T = 1", "1");

            eval("var a = 1 a", "1");

            eval("var a = void", "void");
            eval("var a = void a", "void");

            eval("var a a = 1", "1");
            eval("var a : T a = 1", "1");

            eval("fn () { 1 }()", "1");
            eval("fn () { var a = 1 }()", "1");

            eval("fn (a) { a }(1)", "1");
            eval("fn (a = 1) { a }()", "1");
            eval("fn (a = 1) { a = 2 }()", "2");
            eval("fn (a = 1) { a }(2)", "2");

            eval("fn (a) { fn () { a }() }(3)", "3");
            eval("fn () { fn (a) { a }(3) }()", "3");
            eval("var a = 1 fn (a) { a }(3)", "3");
            eval("var a = 1 fn () { a }()", "1");
            eval("var a = 1 fn (b) { b = 2 }(3)", "2");
            eval("var a = 1 fn (b) { var c = 2 }(3)", "2");

            eval("var a1 = 1 a1", "1");
            eval("var a_1 = 1 a_1", "1");
            eval("var _a_1 = 1 _a_1", "1");

            const String id = "var id = fn (a) { a }";
            eval(id + " fn () { id(3) }()", "3");
            eval(id + " fn (b) { id(b) }(3)", "3");
            eval(id + " fn (a) { id(a) }(3)", "3");
            eval(id + " fn (b) { id(id(b)) }(3)", "3");
            eval(id + " fn (a) { id(id(a)) }(3)", "3");

            eval("fn () { fn (a) { a } }()(4)", "4");
            eval("fn (a) { a(4) }(fn (a) { a })", "4");
            eval(id + " fn (a = id(4)) { a }()", "4");
            eval(id + " fn (a = id(4)) { a }(5)", "5");
            eval(id + " fn (a) { a(4) }(id)", "4");
            eval(id + " fn () { id }()(4)", "4");
            eval(id + " fn () { id(id) }()(4)", "4");
            eval(id + " fn (a = id) { a }()(4)", "4");

            eval("var a = fn () { var c = 1 } a() a()", "1");

            const String op1 = "var op& = fn (a, b) { b }";
            eval(op1 + " op&(5, 6)", "6");
            eval(op1 + " 5 & 6", "6");
            eval(op1 + " 5 & 6 & 7", "7");
            eval(op1 + " var a = op& a(8, 9)", "9");
            eval("var a = fn (c, d) { c } var op^ = a 9 ^ 10", "9");

            const String t = "var t = fn (x) { fn (y) { x } }";
            const String f = " var f = fn (x) { fn (y) { y } }";
            const String and = " var andX = fn (p) { fn (q) { p(q)(p) } }";
            const String or = " var orX = fn (p) { fn (q) { p(p)(q) } }";
            const String ifthen = " var ifthen = fn (p) { fn (a) { fn (b) { p(a)(b) } } }";
            const String not = " var not = fn (b) { ifthen(b)(f)(t) }";
            const String bools = t + f + and + or + ifthen + not;
            eval(bools + " andX(t)(t)", removeVar(t));
            eval(bools + " andX(t)(f)", removeVar(f));
            eval(bools + " andX(f)(t)", removeVar(f));
            eval(bools + " andX(f)(f)", removeVar(f));
            eval(bools + " orX(t)(t)", removeVar(t));
            eval(bools + " orX(t)(f)", removeVar(t));
            eval(bools + " orX(f)(t)", removeVar(t));
            eval(bools + " orX(f)(f)", removeVar(f));
            eval(bools + " not(t)", removeVar(f));
            eval(bools + " not(f)", removeVar(t));
            eval(bools + " not(andX(t)(f))", removeVar(t));
            eval(bools + " not(orX(t)(f))", removeVar(f));
            eval(bools + " andX(not(t))(not(f))", removeVar(f));
            eval(bools + " orX(not(t))(not(f))", removeVar(t));

            const String adder =
                "var adder = fn (a) { var state = a fn() { state = __plus(state, 1) } }";
            eval(adder + " var a = adder(10) a() a()", "12");
            eval(adder + " var a = adder(10) var b = adder(100) a() b() a()", "12");
            eval(adder + " var a = adder(10) var b = adder(100) a() b() a() b()", "102");

            const String plus = "var op+ = fn(a, b) { __plus(a, b) }";
            eval("__plus(1, __plus(2, 3))", "6");
            eval(plus + " 1 + 2 + 3", "6");
            eval(plus + " var p = op+ p(1, p(2, 3))", "6");
            eval(plus + " var op^ = op+ 1 ^ 2 ^ 3", "6");

            eval("true");
            eval("false");
            eval("var a = true", "true");
            eval("var a = true a", "true");

            const String bools2 = "var and = fn(a, b) { __and(a, b) }\n"
                                  + "var or = fn(a, b) { __or(a, b) }\n";
            eval(bools2 + " true and true", "true");
            eval(bools2 + " true and false", "false");
            eval(bools2 + " true or true", "true");
            eval(bools2 + " true or false", "true");
            eval(bools2 + " false or false", "false");
            eval(bools2 + " var t = true var f = false t and f or t", "true");
            eval(bools2 + " true or false and false", "true");
            eval(bools2 + " var a = and a(true, false)", "false");
            eval("[1, 2]");
            eval(plus + " var c = 1 + 2 [1, 2, c]", "[1, 2, 3]");
            eval(plus + " var c = 1 + 2 [c + 1, c + 1, c + 1]", "[4, 4, 4]");
            eval(plus + " var c = 1 + 2 [c = c + 1, c = c + 1, c = c + 1]", "[4, 5, 6]");
            eval(plus + " var c = 3 var a = [c = c + 1, c = c + 1] c = 5 a a", "[4, 5]");

            eval("__rest([1, 2, 3])", "[2, 3]");
            eval("__at([1, 2, 3], 0)", "1");
            eval("__at([1, 2, 3], 2)", "3");
            eval("__add([1, 2], 3)", "[1, 2, 3]");

            eval("struct { }");
            eval("struct { var a = 1 }");
            eval("var S = struct { var a = 1 }", "struct { var a = 1 }");
            eval("var S = struct { var a = 1 } S", "struct { var a = 1 }");
            eval("var S = struct { var a = 1 } new S", "struct { }");
            eval("new struct { var a = 1 }", "struct { }");
            eval("new struct { var a = 1 }.a", "1");

            eval("var S = struct { var constructor = fn () { } } S()",
                "struct { var constructor = fn () { } }()"); // is "S()" without new valid code?
            eval("var S = struct { var constructor = fn () { } } new S()", "struct { }");
            //eval("var S = struct { var a = 1 }\n(new S).a", "1"); not yet parsing braces
            eval("new struct { var a = 1 var constructor = fn (b) { a = b } } (2).a", "2"); 
            eval("var S = struct { var a = 1 } var s = new S s.a", "1");
            eval("var S = struct { var a = 1 } var s = new S s.a", "1");

            const String struct1 = "var s = new struct { var a = new struct { var b = 1 } } ";
            eval(struct1 + "s.a.b", "1");
            eval(struct1 + "s.a.b = 2", "2");
            eval(struct1 + "var sa = s.a sa.b", "1");
            eval(struct1 + "var sa = s.a sa.b = 2", "2");
            eval(struct1 + "var sa = s.a sa.b = 2 s.a.b", "1");
            eval(struct1 + "var ss = s s.a.b = 2 ss.a.b", "1");
            eval(struct1 + "var ss = s ss.a.b = 2 s.a.b", "1");

            eval(struct1 + id + " id(s).a.b", "1");
            eval(struct1 + id + " id(s).a.b = 2 s.a.b", "1");
            eval(struct1 + " fn (a) { a.a.b = 2 } (s) s.a.b", "1");

            eval("var a = 1 /*a = 2*/ a", "1");
            eval("var a = 1 --a = 2\n a", "1");
            eval("var a = 1 /*--a = 2*/ a", "1");
            eval("var a = 1 /*/*a = 2*/ a", "1");
            eval(id + " id/*(1)*/(2)", "2");
            eval(id + " id--(1)\n(2)", "2");
            eval(plus + " 1 + --3 +\n 5", "6");
            eval(plus + " 1 + /*--3 +*/ 5", "6");
            eval(plus + " 1 --+ 3\n + 5", "6");
            eval(plus + " 1 /*+ --3*/ + 5", "6");
        }


        private static String removeVar(String t) => t.SubstringAfter("= ");


        // ReSharper disable once UnusedParameter.Local
        private static void check(IAsi al, String expected, Printer printer)
        {
            var actual = al.Accept(printer);
            if (expected != actual)
                throw new EfektException(
                    "Test Failed - Expected: '" + expected + "' Actual: '" + actual + "'");
        }


        private static void parseWithBraces(String code, String expected)
        {
            parse(code, expected, new Printer {PutBracesAroundBinOpApply = true});
        }


        private static void parse(String code)
        {
            parse(code, code, new Printer());
        }


        private static void parse(String code, String expected)
        {
            parse(code, expected, new Printer());
        }


        private static void parse(String code, String expected, Printer printer)
        {
            var p = new Parser();
            var al = p.Parse(code);
            check(al, expected, printer);
        }


        private static void eval(String code)
        {
            eval(code, code, new Printer());
        }


        private static void eval(String code, String expected)
        {
            eval(code, expected, new Printer());
        }


        private static void eval(String code, String expected, Printer printer)
        {
            var p = new Parser();
            var al = p.Parse(code);
            var i = new Interpreter();
            var asi = i.VisitAsiList(al);
            check(asi, expected, printer);
        }
    }
}