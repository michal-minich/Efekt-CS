﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;


namespace Efekt
{
    public static class Utils
    {
        public static String SubstringAfter(this String value, String after)
        {
            Contract.Requires(value.Length >= after.Length);

            var startIx = value.IndexOf(after);
            Contract.Assume(startIx != -1);
            var ix = startIx + after.Length;
            return value.Substring(ix);
        }


        public static Int32 ToInt(this String value) => Convert.ToInt32(value);


        public static T ParseEnum<T>(this String value) where T : struct
            => (T)Enum.Parse(typeof (T), value);


        public static T Last<T>(this IReadOnlyList<T> source) => source[source.Count - 1];


        public static IEnumerable<T> DropLast<T>(this IEnumerable<T> source)
        {
            var buffer = default(T);
            var buffered = false;

            foreach (var x in source)
            {
                if (buffered)
                    yield return buffer;

                buffer = x;
                buffered = true;
            }
        }


        public static IEnumerable<T> Append<T>(this T first, IEnumerable<T> rest)
        {
            yield return first;
            foreach (var r in rest)
                yield return r;
        }
    }


    public class EqComparer<T> : IEqualityComparer<T>
    {
        readonly Func<T, T, Boolean> comparer;


        public EqComparer(Func<T, T, Boolean> comparer)
        {
            this.comparer = comparer;
        }


        public Boolean Equals(T x, T y) => comparer(x, y);


        public Int32 GetHashCode(T obj) => obj.GetHashCode();
    }
}