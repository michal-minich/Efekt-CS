using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
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
        T VisitGoto(Goto gt);
        T VisitLabel(Label lbl);
        T VisitBreak(Break br);
        T VisitContinue(Continue ct);
        T VisitReturn(Return r);
        T VisitRepeat(Repeat rp);
        T VisitForEach(ForEach fe);
        T VisitThrow(Throw th);
        T VisitTry(Try tr);
        T VisitAssume(Assume asm);
        T VisitAssert(Assert ast);
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


        public T VisitGoto(Goto gt)
        {
            Contract.Requires(gt != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitLabel(Label lbl)
        {
            Contract.Requires(lbl != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitBreak(Break br)
        {
            Contract.Requires(br != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitContinue(Continue ct)
        {
            Contract.Requires(ct != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitReturn(Return r)
        {
            Contract.Requires(r != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitRepeat(Repeat rp)
        {
            Contract.Requires(rp != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitForEach(ForEach fe)
        {
            Contract.Requires(fe != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitThrow(Throw th)
        {
            Contract.Requires(th != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitTry(Try tr)
        {
            Contract.Requires(tr != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitAssume(Assume asm)
        {
            Contract.Requires(asm != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitAssert(Assert ast)
        {
            Contract.Requires(ast != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }
    }


    [ContractClass(typeof (IAsiContract))]
    public interface IAsi
    {
        List<IExp> Attributes { get; set; }
        T Accept<T>(IAsiVisitor<T> v) where T : class;
        Int32 Line { get; }
    }


    [ContractClassFor(typeof (IAsi))]
    internal abstract class IAsiContract : IAsi
    {
        public List<IExp> Attributes
        {
            get
            {
                Contract.Ensures(Contract.Result<List<IExp>>() != null);
                return null;
            }

            set { Contract.Requires(value != null); }
        }


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
                //Contract.Ensures(Contract.Result<Int32>() >= 1);
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
        public List<IExp> Attributes { get; set; } = new List<IExp>();
        public abstract T Accept<T>(IAsiVisitor<T> v) where T : class;
        public Int32 Line { get; set; }
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
        public IReadOnlyList<IAsi> Items { get; set; }


        public AsiList()
        {
            
        }

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
            Line = item.Line;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitErr(this);
    }


    public sealed class Int : Atom
    {
        public BigInteger Value { get; }


        public Int(BigInteger value)
        {
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitInt(this);
    }


    public sealed class Ident : Val
    {
        public String Name { get; }
        public IdentCategory Category { get; }


        public Ident(String name)
        {
            Contract.Requires(name.Length >= 1);

            Name = name;

            var firstChar = name[0];
            if (System.Char.IsLower(firstChar) || firstChar == '_')
                Category = IdentCategory.Value;
            else if (System.Char.IsUpper(firstChar))
                Category = IdentCategory.Type;
            else if (firstChar == '@')
            {
                Contract.Assert(name.Length >= 2);
                Category = IdentCategory.Attribute;
            }
            else Category = IdentCategory.Op;
        }


        public Ident(String name, IdentCategory category)
        {
            Name = name;
            Category = category;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitIdent(this);
    }


    public enum IdentCategory
    {
        Value,
        Type,
        Op,
        Attribute
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
        public Boolean IsVar { get; set; }
        public Ident Ident { get; }

        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        [CanBeNull]
        public IAsi Type { get; }

        [CanBeNull]
        public IExp Value { get; set; }


        public Declr(Ident ident, [CanBeNull] IAsi type, [CanBeNull] IExp value)
        {
            Ident = ident;
            Type = type;
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitDeclr(this);
    }


    public sealed class Arr : Exp
    {
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public List<IExp> Items { get; set; }

        public Boolean IsEvaluated { get; set; }


        public Arr()
        {
            }


        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public Arr(List<IExp> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitArr(this);
    }


    public sealed class Struct : Type, IHasEnv
    {
        public IReadOnlyCollection<IAsi> Items { get; set; }
        public Env Env { get; set; }

        public Struct()
        {
        }

        public Struct(IReadOnlyCollection<IAsi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitStruct(this);
    }

    public sealed class Fn : Val, IHasEnv
    {
        public IReadOnlyList<Declr> Params { get; set; }
        public IReadOnlyList<IAsi> BodyItems { get; set; }
        public Env Env { get; set; }
        public Int32 CountMandatoryParams { get; set; }


        public Fn()
        {
        }


        public Fn(IReadOnlyList<Declr> @params, IReadOnlyList<IAsi> bodyItems)
        {
            Params = @params;
            BodyItems = bodyItems;
            CountMandatoryParams = @params.Count;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitFn(this);
    }


    public sealed class FnApply : Exp
    {
        public IAsi Fn { get; }
        public IReadOnlyCollection<IExp> Args { get; set; }


        public FnApply(IAsi fn)
        {
            Fn = fn;

        }
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
        public IExp Exp { get; set; }


        public New()
        {
        }


        public New(IExp exp)
        {
            Exp = exp;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitNew(this);
    }


    public sealed class Void : Atom
    {
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")] public static readonly Void Instance = new Void();

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


    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "GoTo")]
    public sealed class Goto : Stm
    {
        public Ident LabelName { get; }


        public Goto(Ident labelName)
        {
            LabelName = labelName;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitGoto(this);
    }


    public sealed class Label : Stm
    {
        public Ident LabelName { get; }


        public Label(Ident labelName)
        {
            LabelName = labelName;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitLabel(this);
    }


    public sealed class Break : Stm
    {
        [CanBeNull]
        public IExp Test { get; }


        public Break([CanBeNull] IExp test)
        {
            Test = test;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitBreak(this);
    }


    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "Continue")]
    public sealed class Continue : Stm
    {
        [CanBeNull]
        public IExp Test { get; }


        public Continue([CanBeNull] IExp test)
        {
            Test = test;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitContinue(this);
    }


    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "Return")]
    public sealed class Return : Stm
    {
        [CanBeNull]
        public IAsi Value { get; }


        public Return([CanBeNull] IAsi value)
        {
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitReturn(this);
    }


    public sealed class Repeat : Stm
    {
        public IReadOnlyList<IAsi> Items { get; }


        public Repeat(IReadOnlyList<IAsi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitRepeat(this);
    }


    public sealed class ForEach : Stm
    {
        public Ident Ident { get; }
        public IAsi Iterable { get; }
        public IReadOnlyCollection<IAsi> Items { get; }


        public ForEach(Ident ident, IAsi iterable, IReadOnlyCollection<IAsi> items)
        {
            Ident = ident;
            Iterable = iterable;
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitForEach(this);
    }


    public sealed class Throw : Stm
    {
        public IAsi Ex { get; }


        public Throw(IAsi ex)
        {
            Ex = ex;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitThrow(this);
    }


    public sealed class Try : Stm
    {
        public IReadOnlyList<IAsi> TryItems { get; }

        [CanBeNull]
        public IReadOnlyList<IAsi> CatchItems { get; }

        [CanBeNull]
        public Ident ExVar { get; }

        [CanBeNull]
        public IReadOnlyList<IAsi> FinallyItems { get; }


        public Try(
            IReadOnlyList<IAsi> tryItems,
            [CanBeNull] IReadOnlyList<IAsi> catchItems,
            [CanBeNull] Ident exVar,
            [CanBeNull] IReadOnlyList<IAsi> finallyItems)
        {
            TryItems = tryItems;
            CatchItems = catchItems;
            ExVar = exVar;
            FinallyItems = finallyItems;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitTry(this);
    }


    public sealed class Assume : Stm
    {
        public IAsi Exp { get; }


        public Assume(IAsi exp)
        {
            Exp = exp;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitAssume(this);
    }


    public sealed class Assert : Stm
    {
        public IAsi Exp { get; }


        public Assert(IAsi exp)
        {
            Exp = exp;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitAssert(this);
    }
}