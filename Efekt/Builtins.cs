using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;


namespace Efekt
{
    public static class Builtins
    {
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public static IExp Call(String fnName, IReadOnlyList<IExp> args)
        {
            Contract.Ensures(Contract.Result<IExp>() != null);

            var err = args.FirstOrDefault(a => a is IErr);
            if (err != null)
                return err;

            switch (fnName)
            {
                case "eq":
                    return new Bool(args[0].toString() == args[1].toString());
                case "lt":
                    return new Bool(args[0].asInt() < args[1].asInt());

                case "plus":
                    return new Int((args[0].asInt() + args[1].asInt()).ToString());
                case "multiply":
                    return new Int((args[0].asInt() * args[1].asInt()).ToString());

                case "and":
                    return new Bool(args[0].asBool() && args[1].asBool());
                case "or":
                    return new Bool(args[0].asBool() || args[1].asBool());

                case "rest":
                    return new Arr(args[0].asArr().Items.Skip(1).ToList());
                case "at":
                    return args[0].asArr().Items.ElementAt(args[1].asInt());
                case "add":
                    args[0].asArr().Items.Add(args[1]);
                    return new Void();

                case "env":
                    return printEnvText(args[0]);
                case "print":
                    Console.WriteLine(args[0].toString());
                    return new Void();

                default:
                    throw new EfektException("Unknown builtin: " + fnName);
            }
        }


        static Void printEnvText(IExp asi)
        {
            var he = asi as IHasEnv;
            if (he != null)
                Env.PrintEnv(he.Env);
            return new Void();
        }


        static Int32 asInt(this IExp asi) => ((Int)asi).Value.ToInt();

        static Boolean asBool(this IExp asi) => ((Bool)asi).Value;

        static Arr asArr(this IExp asi) => (Arr)asi;

        static String toString(this IAsi asi) => asi.Accept(Program.DefaultPrinter);
    }
}