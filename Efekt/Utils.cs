using System;


namespace Efekt
{
    public static class Utils
    {
        public static String SubstringAfter(this String value, String after)
        {
            var ix = value.IndexOf(after) + after.Length;
            return value.Substring(ix);
        }


        public static Int32 ToInt(this String value) => Convert.ToInt32(value);
    }
}