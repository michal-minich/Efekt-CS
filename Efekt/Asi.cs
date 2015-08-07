using System;
using System.Collections.Generic;
using JetBrains.Annotations;


namespace Efekt
{
    public interface IAsiVisitor<out T>
    {
        T VisitAsiList(AsiList al);
        T VisitInt(Int ii);
        T VisitIdent(Ident ident);
        T VisitBinOpApply(BinOpApply opa);
        T VisitDeclr(Declr d);
        T VisitArr(Arr arr);
        T VisitStruct(Struct s);
        T VisitFn(Fn fn);
        T VisitFnApply(FnApply fna);
        T VisitNew(New n);
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


    public sealed class Ident : Asi
    {
        public String Value { get; }
        public IdentType Type { get; }


        public Ident(String value, IdentType type)
        {
            Type = type;
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v)
        {
            return v.VisitIdent(this);
        }
    }


    public enum IdentType
    {
        Value,
        Type,
        Op
    }


    public sealed class BinOpApply : Asi
    {
        public Ident Op { get; }
        public Asi Op1 { get; }
        public Asi Op2 { get; set; }


        public BinOpApply(Ident op, Asi op1, Asi op2)
        {
            Op = op;
            Op1 = op1;
            Op2 = op2;
        }


        public override T Accept<T>(IAsiVisitor<T> v)
        {
            return v.VisitBinOpApply(this);
        }
    }


    public sealed class Declr : Asi
    {
        public Ident Ident { get; }

        [CanBeNull]
        public Asi Type { get; }

        [CanBeNull]
        public Asi Value { get; }


        public Declr(Ident ident, [CanBeNull] Asi type, [CanBeNull] Asi value)
        {
            Ident = ident;
            Type = type;
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v)
        {
            return v.VisitDeclr(this);
        }
    }


    public sealed class Arr : Asi
    {
        public IEnumerable<Asi> Items { get; }


        public Arr(IEnumerable<Asi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v)
        {
            return v.VisitArr(this);
        }
    }


    public sealed class Struct : Asi
    {
        public IEnumerable<Asi> Items { get; }


        public Struct(IEnumerable<Asi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v)
        {
            return v.VisitStruct(this);
        }
    }


    public sealed class Fn : Asi
    {
        public IEnumerable<Asi> Params { get; }
        public IEnumerable<Asi> Items { get; }


        public Fn(IEnumerable<Asi> @params, IEnumerable<Asi> items)
        {
            Params = @params;
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v)
        {
            return v.VisitFn(this);
        }
    }


    public sealed class FnApply : Asi
    {
        public Asi Fn { get; }
        public IEnumerable<Asi> Args { get; }


        public FnApply(Asi fn, IEnumerable<Asi> args)
        {
            Fn = fn;
            Args = args;
        }


        public override T Accept<T>(IAsiVisitor<T> v)
        {
            return v.VisitFnApply(this);
        }
    }


    public sealed class New : Asi
    {
        public Ident Ident { get; }


        public New(Ident ident)
        {
            Ident = ident;
        }


        public override T Accept<T>(IAsiVisitor<T> v)
        {
            return v.VisitNew(this);
        }
    }
}