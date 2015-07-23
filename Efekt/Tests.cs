using System;
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
            parse(" 123 ", "123");
            parse(" 123 456", "123\n456");
        }


        private static void parse(String code)
        {
            parse(code, code);
        }


        // ReSharper disable once UnusedParameter.Local
        private static void parse(String code, String expected)
        {
            var p = new Parser();
            var pr = new Printer();

            var asi = p.Parse(code);
            var actual = asi.Accept(pr);

            Contract.Assume(expected == actual);
        }
    }
}