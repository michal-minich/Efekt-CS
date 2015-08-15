﻿using System;
using System.IO;
using System.Linq;


namespace Efekt
{
    internal static class Program
    {
        public static Printer DefaultPrinter;
        public static ValidationList ValidationList;


        internal static void Main(String[] args)
        {
            try
            {
                init();

                Tests.Test();

                if (args.Length != 1)
                {
                    Console.WriteLine("Usage: Efekt <file>");
                    return;
                }

                run(args[0]);
            }
            catch (ValidationException)
            {
                // do nothing, it should be printed already
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
            }
        }


        static void init()
        {
            DefaultPrinter = new Printer();

            var basePath = AppDomain.CurrentDomain.BaseDirectory + @"Resources\";

            ValidationList = ValidationList.InitFrom(
                File.ReadAllLines(basePath + "validations.en-US.ef"));

            var severities = ValidationList.LoadSeverities(
                File.ReadAllLines(basePath + "severity-light.ef"));

            ValidationList.UseSeverities(severities);
        }


        static void run(String filePath)
        {
            var p = new Parser();
            var txt = File.ReadAllText(filePath);
            var al = p.Parse(txt, ValidationList);

            var prelude = p.Parse(preludeCode, ValidationList);
            var code = new AsiList(prelude.Items.Concat(al.Items).ToList());

            Console.WriteLine("Running...");

            var i = new Interpreter();
            var res = i.Run(code, ValidationList);

            var str = res.Accept(DefaultPrinter);
            Console.WriteLine(str);
        }


        const String preludeCode =
            "var id = fn (a) { a }\n"
            + "var op+ = fn(a, b) { __plus(a, b) }\n"
            + "var op* = fn(a, b) { __multiply(a, b) }\n"
            + "var inc = fn(a, b) { __plus(a, 1) }\n"
                //+ "var dec = fn(a, b) { __plus(a, -1) }\n"
            + "var and = fn(a, b) { __and(a, b) }\n"
            + "var or = fn(a, b) { __or(a, b) }\n"
            + "var first = fn(a) { __at(a, 0) }\n"
            + "var rest = fn(a) { __rest(a) }\n"
            + "var at = fn(a, ix) { __at(a, ix) }\n"
            + "var add = fn(a, item) { __add(a, item) }\n"
            + "var env = fn(a) { __env(a) }\n"
            + "var print = fn(a) { __print(a) }\n";
    }
}