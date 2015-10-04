using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using JetBrains.Annotations;


namespace Efekt
{

    [ContractClass(typeof (IAsiContract))]
    public interface IAsi
    {
        List<IExp> Attributes { get; set; }
        T Accept<T>(IAsiVisitor<T> v) where T : class;
        Int32 Line { get; }
    }


    [ContractClassFor(typeof (IAsi))]
    abstract class IAsiContract : IAsi
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
        [CanBeNull] public Declr DeclaredBy { get; set; }
        public List<Ident> UsedBy { get; } = new List<Ident>();


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
            else
                Category = IdentCategory.Op;
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
        public Ident Ident { get; set; }

        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        [CanBeNull]
        public IExp Type { get; set; }

        [CanBeNull]
        public IExp Value { get; set; }


        public Declr()
        {
        }


        public Declr(Ident ident, [CanBeNull] IExp type, [CanBeNull] IExp value)
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


    public interface IRecord : IHasEnv, IAsi
    {
        IReadOnlyList<IAsi> Items { get; set; }
    }


    public sealed class Struct : Type, IRecord
    {
        public IReadOnlyList<IAsi> Items { get; set; }
        public Env Env { get; set; }


        public Struct()
        {
        }


        public Struct(IReadOnlyList<IAsi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitStruct(this);
    }


    public sealed class Class : Type, IRecord
    {
        public IReadOnlyList<IAsi> Items { get; set; }
        public Env Env { get; set; }


        public Class()
        {
        }


        public Class(IReadOnlyList<IAsi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitClass(this);
    }


    public sealed class Fn : Val, IHasEnv
    {
        public IReadOnlyList<Declr> Params { get; set; }
        public IReadOnlyList<IAsi> BodyItems { get; set; }
        public Env Env { get; set; }
        public Int32 CountMandatoryParams { get; set; }
        public IExp ExtensionArg { get; set; }


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
        public IReadOnlyList<IAsi> Items { get; set; }


        public Repeat()
        {
        }


        public Repeat(IReadOnlyList<IAsi> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitRepeat(this);
    }


    public sealed class ForEach : Stm
    {
        public Ident Ident { get; set; }
        public IAsi Iterable { get; set; }
        public IReadOnlyCollection<IAsi> Items { get; set; }


        public ForEach()
        {
        }


        public ForEach(Ident ident, IAsi iterable, IReadOnlyCollection<IAsi> items)
        {
            Ident = ident;
            Iterable = iterable;
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitForEach(this);
    }


    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "Throw")]
    public sealed class Throw : Stm
    {
        [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    [CanBeNull]    public IAsi Ex { get; }


        public Throw(IAsi ex)
        {
            Ex = ex;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitThrow(this);
    }


    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "Try")]
    public sealed class Try : Stm
    {
        public IReadOnlyList<IAsi> TryItems { get; set; }

        [CanBeNull]
        public IReadOnlyList<IAsi> CatchItems { get; set; }

        [CanBeNull]
        public Ident ExVar { get; set; }

        [CanBeNull]
        public IReadOnlyList<IAsi> FinallyItems { get; set; }


        public Try()
        {
        }


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


    public interface ISimpleType : IType
    {
        String Name { get; }
    }


    public sealed class VoidType : Asi, ISimpleType
    {
        public String Name => "Void";

        public static VoidType Instance { get; } = new VoidType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class AnyType : Asi, ISimpleType
    {
        public String Name => "Any";

        public static AnyType Instance { get; } = new AnyType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class ErrType : Asi, ISimpleType
    {
        public String Name => "Err";

        public static ErrType Instance { get; } = new ErrType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class BoolType : Asi, ISimpleType
    {
        public String Name => "Bool";

        public static BoolType Instance { get; } = new BoolType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class IntType : Asi, ISimpleType
    {
        public String Name => "Int";

        public static IntType Instance { get; } = new IntType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class CharType : Asi, ISimpleType
    {
        public String Name => "Char";

        public static CharType Instance { get; } = new CharType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class ArrType : Asi, ISimpleType
    {
        public String Name => "List";

        public static ArrType Instance { get; } = new ArrType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class FnType : Asi, ISimpleType
    {
        public String Name => "Fn";

        public static FnType Instance { get; } = new FnType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class ClassType : Asi, ISimpleType
    {
        public String Name => "Class";

        public static ClassType Instance { get; } = new ClassType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }
}