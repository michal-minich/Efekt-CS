using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        T VisitVoid(Void v);
        T VisitBool(Bool b);
        T VisitChar(Char c);
    }


    public interface IAsi
    {
        T Accept<T>(IAsiVisitor<T> v);
    }


    public abstract class Asi : IAsi
    {
        public abstract T Accept<T>(IAsiVisitor<T> v);


        public override String ToString() => GetType().Name + ": " + Accept(Program.DefaultPrinter);
    }


    public sealed class AsiList : Asi
    {
        public IEnumerable<Asi> Items { get; }


        public AsiList(IEnumerable<Asi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitAsiList(this);
    }


    public sealed class Int : Asi
    {
        public String Value { get; }


        public Int(String value)
        {
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitInt(this);
    }


    public sealed class Ident : Asi
    {
        public String Name { get; }

        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public IdentType Type { get; }


        public Ident(String name, IdentType type)
        {
            Type = type;
            Name = name;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitIdent(this);
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
        public Asi Op1 { get; set; }
        public Asi Op2 { get; set; }


        public BinOpApply(Ident op, Asi op1, Asi op2)
        {
            Op = op;
            Op1 = op1;
            Op2 = op2;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitBinOpApply(this);
    }


    public sealed class Declr : Asi
    {
        public Ident Ident { get; }

        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        [CanBeNull]
        public Asi Type { get; }

        public Boolean IsVar { get; set; }


        public Declr(Ident ident, [CanBeNull] Asi type)
        {
            Ident = ident;
            Type = type;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitDeclr(this);
    }


    public sealed class Arr : Asi
    {
        public IEnumerable<Asi> Items { get; }

        public Boolean IsEvaluated { get; set; }


        public Arr(IEnumerable<Asi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitArr(this);
    }


    public sealed class Struct : Asi
    {
        public IEnumerable<Asi> Items { get; }

        public Env Env { get; set; }


        public Struct(IEnumerable<Asi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitStruct(this);
    }


    public sealed class Fn : Asi
    {
        public IEnumerable<Asi> Params { get; }
        public IEnumerable<Asi> Items { get; }
        public Env Env { get; set; }


        public Fn(IEnumerable<Asi> @params, IEnumerable<Asi> items)
        {
            Params = @params;
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitFn(this);
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


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitFnApply(this);
    }


    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "New")]
    public sealed class New : Asi
    {
        public Asi Exp { get; }


        public New(Asi exp)
        {
            Exp = exp;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitNew(this);
    }


    public sealed class Void : Asi
    {
        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitVoid(this);
    }


    public sealed class Bool : Asi
    {
        public Boolean Value { get; }


        public Bool(Boolean value)
        {
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitBool(this);
    }


    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "Char")]
    public sealed class Char : Asi
    {
        public System.Char Value { get; }


        public Char(System.Char value)
        {
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitChar(this);
    }
}