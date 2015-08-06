﻿using System;
using System.Diagnostics.Contracts;


namespace Efekt
{
    public static class Tests
    {
        public static void TestParser()
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

            Console.WriteLine("All Tests OK");
            Console.ReadLine();
        }


        private static void parseWithBraces(String code, String expected)
        {
            parse(code, expected, new Printer {PutBracesAroundBinOpApply = true});
        }


        private static void parse(String code)
        {
            parse(code, code, new Printer());
        }


        // ReSharper disable once UnusedParameter.Local
        private static void parse(String code, String expected)
        {
            parse(code, expected, new Printer());
        }


        // ReSharper disable once UnusedParameter.Local
        private static void parse(String code, String expected, Printer printer)
        {
            var p = new Parser();

            var asi = p.Parse(code);
            var actual = asi.Accept(printer);

            if (expected != actual)
                throw new Exception();
        }
    }
}