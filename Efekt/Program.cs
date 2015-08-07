using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;


namespace Efekt
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class Program
    {
        public static Printer DefaultPrinter;


        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "args")]
        // ReSharper disable once UnusedParameter.Global
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

                    var i = new Interpreter();
                    i.VisitAsiList(al);
                }
                else
                {
                    Console.WriteLine("Usage: Efekt <file>");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}