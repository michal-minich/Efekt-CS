﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class ValidationType
    {
        public String Code { get; }
        public String Template { get; }
        public ValidationSeverity Severity { get; set; }


        public ValidationType(String code, String template)
        {
            Code = code;
            Template = template;
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public enum ValidationSeverity
    {
        None,
        Hint,
        Suggestion,
        Warning,
        Error
    }


    public sealed class Validation
    {
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public ValidationType Type { get; }

        public IReadOnlyCollection<Object> Items { get; }

        public String Text => Items.Count == 0
            ? Type.Template
            : getShortenedAsiText();


        String getShortenedAsiText()
        {
            var res = Items
                .Select(item => item is IAsi
                    ? ((IAsi)item).Accept(Program.DefaultPrinter)
                    : item.ToString())
                .Select(af => af.Length > 20 ? af.Substring(0, 20) + "..." : af).ToArray();
            return String.Format(Type.Template, res);
        }


        public Validation(
            ValidationType type,
            IReadOnlyCollection<Object> items)
        {
            Type = type;
            Items = items;
        }
    }

    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public sealed class ValidationException : Exception
    {
        public Validation Validation { get; }


        public ValidationException(Validation validation)
        {
            Validation = validation;
        }
    }


    public sealed class ValidationList
    {
        readonly List<Validation> validations = new List<Validation>();

        readonly Dictionary<String, ValidationType> types;


        ValidationList(Dictionary<String, ValidationType> ts)
        {
            types = ts;
        }


        public static ValidationList InitFrom(IEnumerable<String> lines)
            => new ValidationList(extrat(lines).ToDictionary(
                kvp => kvp.Key,
                kvp => new ValidationType(kvp.Key, kvp.Value.Trim('"'))));


        public static Dictionary<String, ValidationSeverity> LoadSeverities(
            IEnumerable<String> lines)
            => extrat(lines).ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ParseEnum<ValidationSeverity>());


        static Dictionary<String, String> extrat(IEnumerable<String> lines)
        {
            var dict = new Dictionary<String, String>();
            foreach (var l in lines)
            {
                if (String.IsNullOrWhiteSpace(l))
                    continue;
                var split = l.Split('=');
                var left = split[0].Trim();
                var right = split[1].Trim();
                dict.Add(left, right);
            }
            return dict;
        }


        Validation add(IAsi item, [CallerMemberName] String code = "")
        {
            var v = new Validation(types[code], new Object[] { item });
            validations.Add(v);
            handle(v);
            return v;
        }


        Validation add(Object[] items, [CallerMemberName] String code = "")
        {
            var v = new Validation(types[code], items);
            validations.Add(v);
            handle(v);
            return v;
        }


        static void handle(Validation v)
        {
            Console.Write(v.Type.Severity + " at ");
            var firstAsi = v.Items.OfType<IAsi>().First();
            Console.Write(firstAsi.Line /* + ":" + v.AffectedItem.Column*/+ " : ");
            Console.WriteLine(v.Text);
            if (v.Type.Severity == ValidationSeverity.Error)
                throw new ValidationException(v);
        }


        public void UseSeverities(Dictionary<String, ValidationSeverity> severities)
        {
            foreach (var svr in severities)
                types[svr.Key].Severity = svr.Value;
        }


        public void NothingAfterIf(IAsi affectedItem) => add(affectedItem);
        public void IfTestIsNotExp(IAsi affectedItem) => add(affectedItem);
        public void ExpHasNoEffect(IAsi affectedItem) => add(affectedItem);
        public void ImplicitVar(Ident affectedItem) => add(affectedItem);
        public void DeclrExpected(IAsi affectedItem) => add(affectedItem);
        public void ImportIsNotStruct(IExp affectedItem) => add(affectedItem);
        public void ImportIsStructType(IExp affectedItem) => add(affectedItem);
        public void CannotImportTo(Import affectedItem) => add(affectedItem);
        public void IfTestIsNotBool(IAsi affectedItem) => add(affectedItem);


        public void CannotApply(IAsi affectedItem, IAsi evaluatedItem)
            => add(new[] { affectedItem, evaluatedItem });


        public void WrongParamsOrder(IAsi mandatoryParam, IAsi optionalParam)
            => add(new[] { mandatoryParam, optionalParam });


        public void NotEnoughArgs(IAsi missingParam, IAsi fn, Int32 paramsCount,
            Int32 mandatoryCount, Int32 applyCount)
            => add(new Object[] { missingParam, fn, paramsCount, mandatoryCount, applyCount });


        public void TooManyArgs(IAsi missingParam, IAsi fn, Int32 paramsCount, Int32 applyCount)
            => add(new Object[] { missingParam, fn, paramsCount, applyCount });


        public void ExpectedIdent(BinOpApply affectedItem) => add(affectedItem);
        public void NoStructAfterNew(IAsi asi, String asiTpe) => add(new Object[] { asi, asiTpe });
        public void InstanceAfterNew(IExp exp) => add(exp);
        public void NoConstructor(New n) => add(n);
        public void ConstructorIsNotFn(IAsi asi) => add(asi);
        public void ConstructorNotCalled(New n) => add(n);
        public void ThisMemberAcces(Ident @this) => add(@this);
    }
}