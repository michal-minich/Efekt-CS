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
                    return new Int((args[0].toInt() + args[1].toInt()).ToString());
                default:
                    throw new EfektException("Unknown builtin: " + fnName);
            }
        }


        private static Int32 toInt(this IAsi asi) => asi.toString().ToInt();

        private static String toString(this IAsi asi) => asi.Accept(Program.DefaultPrinter);
    }
}