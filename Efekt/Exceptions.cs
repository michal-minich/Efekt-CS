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


    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    [Serializable]
    public sealed class UnexpectedException : Exception
    {
        public UnexpectedException()
            : base("Unexpected point of execution.")
        {
        }


        public UnexpectedException(String message)
            : base(message)
        {
        }


        public UnexpectedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}