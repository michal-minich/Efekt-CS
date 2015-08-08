using System;
using System.Diagnostics.CodeAnalysis;


namespace Efekt
{
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
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