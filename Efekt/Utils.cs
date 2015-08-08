using System;
using System.Diagnostics.CodeAnalysis;


namespace Efekt
{
    public static class Utils
    {
        public static String SubstringAfter(this String value, String after)
        {
            var ix = value.IndexOf(after) + after.Length;
            return value.Substring(ix);
        }


        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames",
            MessageId = "int")]
        public static Int32 ToInt(this String value) => Convert.ToInt32(value);
    }
}