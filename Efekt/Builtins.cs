using System;
using System.Linq;


namespace Efekt
{
    public static class Builtins
    {
        public static Asi Call(String fnName, params Asi[] args)
        {
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


        private static Void printEnvText(Asi asi)
        {
            var fn = asi as Fn;
            if (fn != null)
                Env.PrintEnv(fn.Env);

            var s = asi as Struct;
            if (s != null)
                Env.PrintEnv(s.Env);

            return new Void();
        }


        private static Int32 asInt(this IAsi asi) => ((Int) asi).Value.ToInt();

        private static Boolean asBool(this IAsi asi) => ((Bool) asi).Value;

        private static Arr asArr(this IAsi asi) => (Arr) asi;

        private static String toString(this IAsi asi) => asi.Accept(Program.DefaultPrinter);
    }
}