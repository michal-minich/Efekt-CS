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
            //eval("");
            eval("1");
            eval("var a = 1", "1");
            eval("var a : T = 1", "1");

            eval("var a a = 1", "1");
            eval("var a : T a = 1", "1");
        }


        // ReSharper disable once UnusedParameter.Local
        private static void check(IAsi al, String expected, Printer printer)
        {
            var actual = al.Accept(printer);

            if (expected != actual)
                throw new Exception();
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


        // ReSharper disable once UnusedParameter.Local
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