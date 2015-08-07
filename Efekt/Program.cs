using System;
using System.Diagnostics.CodeAnalysis;


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
            DefaultPrinter = new Printer();

            Tests.TestParser();
        }
    }
}