using System;
using System.Collections.Generic;


namespace Efekt
{
    public interface IAsiVisitor<out T>
    {
        T VisitAsiList(AsiList al);
        T VisitInt(Int ii);
    }


    public interface IAsi
    {
        T Accept<T>(IAsiVisitor<T> v);
    }


    public abstract class Asi : IAsi
    {
        public abstract T Accept<T>(IAsiVisitor<T> v);


        public override String ToString()
        {
            return GetType().Name + ": " + Accept(Program.DefaultPrinter);
        }
    }


    public sealed class AsiList : Asi
    {
        public IEnumerable<Asi> Items { get; }


        public AsiList(IEnumerable<Asi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v)
        {
            return v.VisitAsiList(this);
        }
    }


    public sealed class Int : Asi
    {
        public String Value { get; }


        public Int(String value)
        {
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v)
        {
            return v.VisitInt(this);
        }
    }
}