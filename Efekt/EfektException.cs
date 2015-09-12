using System;
using System.Diagnostics.CodeAnalysis;


namespace Efekt
{
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [Serializable]
    public sealed class EfektException : Exception
    {
        public EfektException(String message)
            : base(message)
        {
        }


        public EfektException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}