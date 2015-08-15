using System;
using System.Collections.Generic;
using System.ComponentModel;


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


        public static String GetEnumDescription<T>(this T e) where T : struct
        {
            var attrs = typeof (T).GetMember(e.ToString())[0]
                .GetCustomAttributes(typeof (DescriptionAttribute), false);
            return ((DescriptionAttribute)attrs[0]).Description;
        }


        public static T ParseEnum<T>(this String value) where T : struct
            => (T)Enum.Parse(typeof (T), value);


        public static T Last<T>(this IReadOnlyList<T> source) => source[source.Count - 1];
    }
}