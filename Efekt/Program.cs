using System;
using System.IO;
using System.Linq;


namespace Efekt
{
    internal static class Program
    {
        public static Printer DefaultPrinter;


        internal static void Main(String[] args)
        {
            try
            {
                DefaultPrinter = new Printer();

                Tests.Test();

                if (args.Length == 1)
                {
                    var p = new Parser();
                    var txt = File.ReadAllText(args[0]);
                    var al = p.Parse(txt);

                    var prelude = p.Parse(preludeCode);
                    var code = new AsiList(prelude.Items.Union(al.Items));

                    var i = new Interpreter();
                    var res = i.VisitAsiList(code);

                    var str = res.Accept(DefaultPrinter);
                    Console.WriteLine(str);
                }
                else
                {
                    Console.WriteLine("Usage: Efekt <file>");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
            }
        }


        private const String preludeCode =
            "var id = fn (a) { a }\n"
            + "var op+ = fn(a, b) { __plus(a, b) }\n";
    }
}