using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;


namespace Efekt
{
    public static class Builtins
    {
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static IExp Call(String fnName, params IExp[] args)
        {
            var err = args.FirstOrDefault(a => a is IErr);
            if (err != null)
                return err;

            switch (fnName)
            {
                case "plus":
                    return new Int((args[0].asInt() + args[1].asInt()).ToString());
                case "multiply":
                    return new Int((args[0].asInt() * args[1].asInt()).ToString());

                case "and":
                    return new Bool(args[0].asBool() && args[1].asBool());
                case "or":
                    return new Bool(args[0].asBool() || args[1].asBool());

                case "rest":
                    return new Arr(args[0].asArr().Items.Skip(1));
                case "at":
                    return args[0].asArr().Items.ElementAt(args[1].asInt());
                case "add":
                    return new Arr(args[0].asArr().Items.Union(new[] {args[1]}));

                case "env":
                    return printEnvText(args[0]);
                case "print":
                    Console.WriteLine(args[0].toString());
                    return new Void();

                default:
                    throw new EfektException("Unknown builtin: " + fnName);
            }
        }


        private static Void printEnvText(IExp asi)
        {
            var fn = asi as Fn;
            if (fn != null)
                Env.PrintEnv(fn.Env);

            var s = asi as Struct;
            if (s != null)
                Env.PrintEnv(s.Env);

            return new Void();
        }


        private static Int32 asInt(this IExp asi) => ((Int) asi).Value.ToInt();

        private static Boolean asBool(this IExp asi) => ((Bool) asi).Value;

        private static Arr asArr(this IExp asi) => (Arr) asi;

        private static String toString(this IAsi asi) => asi.Accept(Program.DefaultPrinter);
    }
}