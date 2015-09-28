using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;


namespace Efekt
{
    public sealed class Builtins
    {
        readonly ValidationList validations;


        public Builtins(ValidationList validations)
        {
            this.validations = validations;
        }


        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public IExp Call(String fnName, IReadOnlyList<IExp> args)
        {
            Contract.Ensures(Contract.Result<IExp>() != null);

            var err = args.FirstOrDefault(a => a is IErr);
            if (err != null)
                return err;

            switch (fnName)
            {
                case "eq":
                    return new Bool(args[0].AsiToString() == args[1].AsiToString());
                case "lt":
                    return new Bool(args[0].AsInt() < args[1].AsInt());

                case "plus":
                    return new Int((args[0].AsInt() + args[1].AsInt()));
                case "multiply":
                    return new Int((args[0].AsInt() * args[1].AsInt()));

                case "and":
                    return new Bool(args[0].AsBool() && args[1].AsBool());
                case "or":
                    return new Bool(args[0].AsBool() || args[1].AsBool());

                case "at":
                    return args[0].AsArr().Items.ElementAt(args[1].AsInt());
                case "count":
                    return new Int(args[0].AsArr().Items.Count);

                case "env":
                    return printEnvText(args[0]);
                case "print":
                    Console.WriteLine(args[0].AsiToString());
                    return Void.Instance;
                case "typeof":
                    return args[0].Accept(new TypeInferer());

                default:
                    validations.GenericWarning("Unknown builtin: " + fnName, Void.Instance);
                    return Void.Instance;
            }
        }


        static Void printEnvText(IExp asi)
        {
            var he = asi as IHasEnv;
            if (he != null)
                Env.PrintEnv(he.Env);
            return new Void();
        }
    }


    public static class BuiltinsExtensions
    {
        public static Int32 AsInt(this IExp asi) => (Int32)((Int)asi).Value;

        public static Boolean AsBool(this IExp asi) => ((Bool)asi).Value;

        public static Arr AsArr(this IExp asi) => (Arr)asi;


        public static String AsiToString(this IAsi asi)
            => asi.Accept(Program.DefaultPrinter).Trim('"');
    }
}