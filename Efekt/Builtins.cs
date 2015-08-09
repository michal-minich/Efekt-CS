using System;


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
                case "and":
                    return new Bool(args[0].asBool() && args[1].asBool());
                case "or":
                    return new Bool(args[0].asBool() || args[1].asBool());
                default:
                    throw new EfektException("Unknown builtin: " + fnName);
            }
        }


        private static Int32 asInt(this IAsi asi) => ((Int) asi).Value.ToInt();

        private static Boolean asBool(this IAsi asi) => ((Bool) asi).Value;

        private static String toString(this IAsi asi) => asi.Accept(Program.DefaultPrinter);
    }
}