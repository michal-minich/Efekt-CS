using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;


namespace Efekt
{
    [ContractClass(typeof (IAsiVisitorContract<>))]
    public interface IAsiVisitor<out T> where T : class
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
        T VisitAssign(Assign a);
    }

    [ContractClassFor(typeof (IAsiVisitor<>))]
    internal abstract class IAsiVisitorContract<T> : IAsiVisitor<T> where T : class
    {
        T IAsiVisitor<T>.VisitAsiList(AsiList al)
        {
            Contract.Requires(al != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitErr(Err err)
        {
            Contract.Requires(err != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitInt(Int ii)
        {
            Contract.Requires(ii != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitIdent(Ident i)
        {
            Contract.Requires(i != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitBinOpApply(BinOpApply opa)
        {
            Contract.Requires(opa != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitDeclr(Declr d)
        {
            Contract.Requires(d != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitArr(Arr arr)
        {
            Contract.Requires(arr != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitStruct(Struct s)
        {
            Contract.Requires(s != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitFn(Fn fn)
        {
            Contract.Requires(fn != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitFnApply(FnApply fna)
        {
            Contract.Requires(fna != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitNew(New n)
        {
            Contract.Requires(n != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitVoid(Void v)
        {
            Contract.Requires(v != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitBool(Bool b)
        {
            Contract.Requires(b != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitChar(Char c)
        {
            Contract.Requires(c != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitIf(If iff)
        {
            Contract.Requires(iff != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitImport(Import imp)
        {
            Contract.Requires(imp != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitAssign(Assign a)
        {
            Contract.Requires(a != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }
    }


    [ContractClass(typeof (IAsiContract))]
    public interface IAsi
    {
        T Accept<T>(IAsiVisitor<T> v) where T : class;
        Int32 Line { get; }

        Int32 Column { get; }
    }


    [ContractClassFor(typeof (IAsi))]
    internal abstract class IAsiContract : IAsi
    {
        T IAsi.Accept<T>(IAsiVisitor<T> v)
        {
            Contract.Requires(v != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        Int32 IAsi.Line
        {
            get
            {
                Contract.Ensures(Contract.Result<Int32>() >= 1);
                return 1;
            }
        }

        Int32 IAsi.Column
        {
            get
            {
                Contract.Ensures(Contract.Result<Int32>() >= 1);
                return 1;
            }
        }
    }


    public interface IHasEnv
    {
        Env Env { get; set; }
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
        public abstract T Accept<T>(IAsiVisitor<T> v) where T : class;
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
        public IReadOnlyList<IAsi> Items { get; }


        public AsiList(IReadOnlyList<IAsi> items)
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


    public sealed class Arr : Exp
    {
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public List<IExp> Items { get; }

        public Boolean IsEvaluated { get; set; }


        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public Arr(List<IExp> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitArr(this);
    }


    public sealed class Struct : Type, IHasEnv
    {
        public IReadOnlyCollection<IAsi> Items { get; }
        public Env Env { get; set; }


        public Struct(IReadOnlyCollection<IAsi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitStruct(this);
    }

    public sealed class Fn : Val, IHasEnv
    {
        public IReadOnlyList<IExp> Params { get; }
        public IReadOnlyList<IAsi> Items { get; }
        public Env Env { get; set; }
        public Int32 CountMandatoryParams { get; set; }


        public Fn(IReadOnlyList<IExp> @params, IReadOnlyList<IAsi> items)
        {
            Params = @params;
            Items = items;
            CountMandatoryParams = @params.Count;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitFn(this);
    }


    public sealed class FnApply : Exp
    {
        public IAsi Fn { get; }
        public IReadOnlyCollection<IExp> Args { get; }


        public FnApply(IAsi fn, IReadOnlyCollection<IExp> args)
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


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitIf(this);
    }


    public sealed class Assign : Exp
    {
        public IExp Target { get; set; }
        public IExp Value { get; }


        public Assign(IExp target, IExp value)
        {
            Target = target;
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitAssign(this);
    }


    public sealed class Import : Stm
    {
        public IExp QualifiedIdent { get; set; }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitImport(this);
    }
}