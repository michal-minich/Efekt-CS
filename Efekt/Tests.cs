using System;


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
            const String and = " var and = fn (p) { fn (q) { p(q)(p) } }";
            const String or = " var or = fn (p) { fn (q) { p(p)(q) } }";
            const String ifthen = " var ifthen = fn (p) { fn (a) { fn (b) { p(a)(b) } } }";
            const String not = " var not = fn (b) { ifthen(b)(f)(t) }";
            const String bools = t + f + and + or + ifthen + not;
            eval(bools + " and(t)(t)", removeVar(t));
            eval(bools + " and(t)(f)", removeVar(f));
            eval(bools + " and(f)(t)", removeVar(f));
            eval(bools + " and(f)(f)", removeVar(f));
            eval(bools + " or(t)(t)", removeVar(t));
            eval(bools + " or(t)(f)", removeVar(t));
            eval(bools + " or(f)(t)", removeVar(t));
            eval(bools + " or(f)(f)", removeVar(f));
            eval(bools + " not(t)", removeVar(f));
            eval(bools + " not(f)", removeVar(t));
            eval(bools + " not(and(t)(f))", removeVar(t));
            eval(bools + " not(or(t)(f))", removeVar(f));
            eval(bools + " and(not(t))(not(f))", removeVar(f));
            eval(bools + " or(not(t))(not(f))", removeVar(t));

            const String adder = "var adder = fn (a) { var state = a fn() { state = __plus(state, 1) } }";
            eval(adder + " var a = adder(10) a() a()", "12");
            eval(adder + " var a = adder(10) var b = adder(100) a() b() a()", "12");
            eval(adder + " var a = adder(10) var b = adder(100) a() b() a() b()", "102");

            const String plus = "var op+ = fn(a, b) { __plus(a, b) }";
            eval("__plus(1, __plus(2, 3))", "6");
            eval(plus + " 1 + 2 + 3", "6");
            eval(plus + " var p = op+ p(1, p(2, 3))", "6");
            eval(plus + " var op^ = op+ 1 ^ 2 ^ 3", "6");
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


        private static void evalWithBraces(String code, String expected)
        {
            eval(code, expected, new Printer {PutBracesAroundBinOpApply = true});
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