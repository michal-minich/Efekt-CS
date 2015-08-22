using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Efekt
{
    internal static class Program
    {
        public static Printer DefaultPrinter;
        public static ValidationList ValidationList;

        static readonly String basePath = AppDomain.CurrentDomain.BaseDirectory;
        static readonly String resPath = basePath + @"Resources\";
        static readonly String libPath = basePath + @"Lib\";


        internal static void Main(String[] args)
        {
            try
            {
                init();

                Tests.Test();

                if (args.Length == 0)
                {
                    Console.WriteLine("Usage: Efekt <file> [<file>] ... ");
                    return;
                }

                run(args);
            }
            catch (ValidationException)
            {
                // do nothing, it should be printed already
            }
            catch (EfektException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (InterpretedThrowException ex)
            {
                Console.Write("Application Exception: ");
                Console.WriteLine(ex.Message);
            }

            catch (Exception ex)
            {
                Console.WriteLine("Unexpected " + ex.GetType().Name + ": " + ex.Message);
            }
        }


        static void init()
        {
            DefaultPrinter = new Printer();

            ValidationList = ValidationList.InitFrom(
                File.ReadAllLines(resPath + "validations.en-US.ef"));

            var severities = ValidationList.LoadSeverities(
                File.ReadAllLines(resPath + "severity-light.ef"));

            ValidationList.UseSeverities(severities);
        }


        static void run(IEnumerable<String> filePaths)
        {
            var p = new Parser();
            var modules = new Dictionary<String, IReadOnlyList<IAsi>>();
            foreach (var path in filePaths.Reverse())
            {
                var txt = File.ReadAllText(path);
                var al = p.Parse(txt, ValidationList);
                modules.Add(Path.GetFileNameWithoutExtension(path), al.Items);
            }

            var preludeTxt = File.ReadAllText(libPath + "prelude.ef");
            var preludeTxtAl = p.Parse(preludeTxt, ValidationList);

            var rw = new Rewriter();
            var prog = rw.MakeProgram(preludeTxtAl.Items, modules);

            var i = new Interpreter();
            var res = i.Run(prog, ValidationList);
            Console.Write("Result: ");
            Console.WriteLine(res.Accept(DefaultPrinter));
        }
    }
}