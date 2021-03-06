﻿using System;
using System.Collections;
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
        IReadOnlyList<Ident> Attributes { get; set; }
        T Accept<T>(IAsiVisitor<T> v) where T : class;
        Int32 Line { get; }

        [CanBeNull]
        Type InfType { get; }
    }


    [ContractClassFor(typeof (IAsi))]
    abstract class IAsiContract : IAsi
    {
        public IReadOnlyList<Ident> Attributes
        {
            get
            {
                Contract.Ensures(Contract.Result<IReadOnlyList<Ident>>() != null);
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

        public abstract Type InfType { get; }
    }


    public interface IHasEnv
    {
        Env Env { get; }
    }


    public abstract class SimpleType : Type
    {
        public abstract String Name { get; }
    }


    public abstract class Asi : IAsi
    {
        public IReadOnlyList<Ident> Attributes { get; set; }
        public abstract T Accept<T>(IAsiVisitor<T> v) where T : class;
        public Int32 Line { get; set; }
        public Type InfType { get; set; }
        public override String ToString() => GetType().Name + ": " + Accept(Program.DefaultPrinter);
    }


    public abstract class Exp : Asi
    {
    }


    public abstract class Stm : Asi
    {
    }


    public abstract class Type : Exp
    {
    }


    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly",
        MessageId = "Val")]
    public abstract class Val : Exp
    {
    }


    public abstract class Atom : Val
    {
    }


    public sealed class Sequence : Asi, IReadOnlyList<IAsi>
    {
        List<IAsi> list;


        public Sequence()
        {
        }


        public Sequence(List<IAsi> list)
        {
            this.list = list;
        }


        public IEnumerator<IAsi> GetEnumerator() => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

        public Int32 Count => list.Count;

        public IAsi this[Int32 index] => list[index];


        public void Init(List<IAsi> items)
        {
            Contract.Assume(list == null);
            list = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSequence(this);
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
        public Declr DeclaredBy { get; set; }
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
        public Exp Op1 { get; }
        public Exp Op2 { get; set; }


        public BinOpApply(Ident op, Exp op1, Exp op2)
        {
            Op = op;
            Op1 = op1;
            Op2 = op2;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitBinOpApply(this);
    }


    public sealed class Declr : Exp, IClassItem
    {
        public Boolean IsVar { get; set; }
        public Ident Ident { get; set; }

        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        [CanBeNull]
        public Exp Type { get; set; }

        [CanBeNull]
        public Exp Value { get; set; }


        public Declr()
        {
        }


        public Declr(Ident ident, [CanBeNull] Exp type, [CanBeNull] Exp value)
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
        public IReadOnlyList<Exp> Items { get; set; }

        public Boolean IsEvaluated { get; set; }


        public Arr()
        {
        }


        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public Arr(IReadOnlyList<Exp> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitArr(this);
    }


    public interface IClassItem : IAsi
    {
    }


    public sealed class Class : Type, IHasEnv
    {
        public IReadOnlyList<IClassItem> Items { get; set; }
        public Env Env { get; set; }


        public Class()
        {
        }


        public Class(IReadOnlyList<IClassItem> items)
        {
            Items = items;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitClass(this);
    }


    public sealed class Fn : Val, IHasEnv
    {
        public IReadOnlyList<Declr> Params { get; set; }
        public Sequence Body { get; set; }
        public Env Env { get; set; }
        public Int32 CountMandatoryParams { get; set; }
        public Exp ExtensionArg { get; set; }


        public Fn()
        {
        }


        public Fn(IReadOnlyList<Declr> @params, Sequence body)
        {
            Params = @params;
            Body = body;
            CountMandatoryParams = @params.Count;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitFn(this);
    }


    public sealed class FnApply : Exp
    {
        public Exp Fn { get; }
        public IReadOnlyList<Exp> Args { get; set; }


        public FnApply(Exp fn)
        {
            Fn = fn;
        }


        public FnApply(Exp fn, IReadOnlyList<Exp> args)
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
        public Exp Exp { get; set; }


        public New()
        {
        }


        public New(Exp exp)
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
        public Exp Test { get; set; }
        public Sequence Then { get; set; }

        [CanBeNull]
        public Sequence Otherwise { get; set; }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitIf(this);
    }


    public sealed class Assign : Exp
    {
        public Exp Target { get; }
        public Exp Value { get; }


        public Assign(Exp target, Exp value)
        {
            Target = target;
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitAssign(this);
    }


    public sealed class Import : Stm, IClassItem
    {
        public Exp QualifiedIdent { get; set; }


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
        public Exp Test { get; }


        public Break([CanBeNull] Exp test)
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
        public Exp Test { get; }


        public Continue([CanBeNull] Exp test)
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
        public Exp Value { get; }


        public Return([CanBeNull] Exp value)
        {
            Value = value;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitReturn(this);
    }


    public sealed class Repeat : Stm
    {
        public Sequence Sequence { get; set; }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitRepeat(this);
    }


    public sealed class ForEach : Stm
    {
        public Ident Ident { get; set; }
        public Exp Iterable { get; set; }
        public Sequence Sequence { get; set; }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitForEach(this);
    }


    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "Throw")]
    public sealed class Throw : Stm
    {
        [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
        [CanBeNull]
        public Exp Ex { get; }


        public Throw(Exp ex)
        {
            Ex = ex;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitThrow(this);
    }


    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords",
        MessageId = "Try")]
    public sealed class Try : Stm
    {
        public Sequence TrySequence { get; set; }

        [CanBeNull]
        public Sequence CatchSequence { get; set; }

        [CanBeNull]
        public Ident ExVar { get; set; }

        [CanBeNull]
        public Sequence FinallySequence { get; set; }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitTry(this);
    }


    public sealed class Assume : Stm
    {
        public Exp Exp { get; }


        public Assume(Exp exp)
        {
            Exp = exp;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitAssume(this);
    }


    public sealed class Assert : Stm
    {
        public Exp Exp { get; }


        public Assert(Exp exp)
        {
            Exp = exp;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitAssert(this);
    }


    public sealed class VoidType : SimpleType
    {
        public override String Name => "Void";

        public static VoidType Instance { get; } = new VoidType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class AnyType : SimpleType
    {
        public override String Name => "Any";

        public static AnyType Instance { get; } = new AnyType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class BoolType : SimpleType
    {
        public override String Name => "Bool";

        public static BoolType Instance { get; } = new BoolType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class IntType : SimpleType
    {
        public override String Name => "Int";

        public static IntType Instance { get; } = new IntType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class CharType : SimpleType
    {
        public override String Name => "Char";

        public static CharType Instance { get; } = new CharType();

        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitSimpleType(this);
    }


    public sealed class ArrType : Type
    {
        public Type ElementType { get; }


        public ArrType(Type elementType)
        {
            ElementType = elementType;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitArrType(this);
    }


    public sealed class OrType : Type
    {
        public IReadOnlyList<Type> Choices { get; }


        public OrType(IReadOnlyList<Type> choices)
        {
            Choices = choices;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitOrType(this);
    }


    public sealed class FnType : Type
    {
        public IReadOnlyList<Type> ParamTypes { get; }
        public Type ReturnType { get; }


        public FnType(IReadOnlyList<Type> paramTypes, Type returnType)
        {
            ParamTypes = paramTypes;
            ReturnType = returnType;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitFnType(this);
    }


    public sealed class ClassType : Type
    {
        public Declr DeclaredBy { get; }


        public ClassType(Declr declaredBy)
        {
            DeclaredBy = declaredBy;
        }


        public override T Accept<T>(IAsiVisitor<T> v) => v.VisitClassType(this);
    }
}