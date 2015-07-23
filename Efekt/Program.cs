using System;


namespace Efekt
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class Program
    {
        public static Printer DefaultPrinter;


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "args")]
        internal static void Main(String[] args)
        {
            DefaultPrinter = new Printer();
            
            Tests.TestParser();

            Console.WriteLine("Finished");
            Console.ReadLine();
        }
    }
}