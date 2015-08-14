using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;


namespace Efekt
{
    public interface IAsiVisitor<out T>
    {
        T VisitAsiList(AsiList al);
        T VisitErr(Err err);
        T VisitInt(Int ii);
        T VisitIdent(Ident i);
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
        T VisitIf(If iff);
        T VisitImport(Import imp);
    }


    public interface IAsi
    {
        T Accept<T>(IAsiVisitor<T> v);
        Int32 Line { get; }

        Int32 Column { get; }
    }


    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    public interface IExp : IAsi
    {
    }


    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    public interface IStm : IAsi
    {
    }


    public interface IType : IExp
    {
    }


    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly",
        MessageId = "Val")]
    public interface IVal : IExp
    {
    }

    public interface IAtom : IVal
    {
    }


    public interface IErr : IStm, IType, IAtom
    {
    }


    public abstract class Asi : IAsi
    {
        public abstract T Accept<T>(IAsiVisitor<T> v);
        public Int32 Line { get; set; }
        public Int32 Column { get; set; }
        public override String ToString() => GetType().Name + ": " + Accept(Program.DefaultPrinter);
    }


    public abstract class Exp : Asi, IExp
    {
    }


    public abstract class Stm : Asi, IStm
    {
    }


    public abstract class Type : Exp, IType
    {
    }


    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly",
        MessageId = "Val")]
    public abstract class Val : Exp, IVal
    {
    }


    public abstract class Atom : Val, IAtom
    {
    }


    public sealed class AsiList : Asi
    {
        public IEnumerable<IAsi> Items { get; }


        public AsiList(IEnumerable<IAsi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitAsiList(this);
    }


    public sealed class Err : Asi, IErr
    {
        [CanBeNull]
        public IAsi Item { get; }


        public Err()
        {
        }


        public Err(IAsi item)
        {
            Item = item;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitErr(this);
    }


    public sealed class Int : Atom
    {
        public String Value { get; }


        public Int(String value)
        {
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitInt(this);
    }


    public sealed class Ident : Val
    {
        public String Name { get; }
        public IdentCategory Category { get; }


        public Ident(String name, IdentCategory category)
        {
            Category = category;
            Name = name;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitIdent(this);
    }


    public enum IdentCategory
    {
        Value,
        Type,
        Op
    }


    public sealed class BinOpApply : Exp
    {
        public Ident Op { get; }
        public IExp Op1 { get; set; }
        public IExp Op2 { get; set; }


        public BinOpApply(Ident op, IExp op1, IExp op2)
        {
            Op = op;
            Op1 = op1;
            Op2 = op2;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitBinOpApply(this);
    }


    public sealed class Declr : Exp
    {
        public Ident Ident { get; }

        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        [CanBeNull]
        public IAsi Type { get; }

        public Boolean IsVar { get; set; }


        public Declr(Ident ident, [CanBeNull] IAsi type)
        {
            Ident = ident;
            Type = type;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitDeclr(this);
    }


    public sealed class Arr : Val
    {
        public IEnumerable<IExp> Items { get; }
        public Boolean IsEvaluated { get; set; }


        public Arr(IEnumerable<IExp> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitArr(this);
    }


    public sealed class Struct : Type
    {
        public IEnumerable<IAsi> Items { get; }
        public Env Env { get; set; }


        public Struct(IEnumerable<IAsi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitStruct(this);
    }

    public sealed class Fn : Val
    {
        public IEnumerable<IExp> Params { get; }
        public IEnumerable<IAsi> Items { get; }
        public Env Env { get; set; }


        public Fn(IEnumerable<IExp> @params, IEnumerable<IAsi> items)
        {
            Params = @params;
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitFn(this);
    }


    public sealed class FnApply : Exp
    {
        public IAsi Fn { get; }
        public IEnumerable<IExp> Args { get; }


        public FnApply(IAsi fn, IEnumerable<IExp> args)
        {
            Fn = fn;
            Args = args;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitFnApply(this);
    }


    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "New")]
    public sealed class New : Exp
    {
        public IExp Exp { get; }


        public New(IExp exp)
        {
            Exp = exp;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitNew(this);
    }


    public sealed class Void : Atom
    {
        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitVoid(this);
    }


    public sealed class Bool : Atom
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
    public sealed class Char : Atom
    {
        public System.Char Value { get; }


        public Char(System.Char value)
        {
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitChar(this);
    }


    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "If")]
    public sealed class If : Exp
    {
        public IExp Test { get; set; }
        public IAsi Then { get; set; }

        [CanBeNull]
        public IAsi Otherwise { get; set; }


        public If()
        {
        }


        public If(IExp test, IAsi then, [CanBeNull] IAsi otherwise)
        {
            Test = test;
            Then = then;
            Otherwise = otherwise;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitIf(this);
    }


    public sealed class Import : Stm
    {
        public IExp QualifiedIdent { get; set; }


        public Import()
        {
        }


        public Import(IExp qualifiedIdent)
        {
            QualifiedIdent = qualifiedIdent;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitImport(this);
    }
}