using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class ValidationType
    {
        public ValidationCategory Category { get; }
        public Int32 Number { get; }
        public String Template { get; }
        public String Code { get; }
        public ValidationSeverity Severity { get; }


        public ValidationType(ValidationCategory category, Int32 number, String template)
        {
            Contract.Requires(number >= 100 && number <= 999);

            Category = category;
            Number = number;
            Template = template;
            Code = String.Concat(Category.GetEnumDescription(), "-", Number.ToString());
        }
    }


    public enum ValidationCategory
    {
        [Description("P")] Parsing,
        [Description("R")] Runtime
    }

    public enum ValidationSeverity
    {
        [Description("0")] None,
        [Description("H")] Hint,
        [Description("S")] Suggestion,
        [Description("W")] Warning,
        [Description("E")] Error
    }


    public sealed class Validation
    {
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public ValidationType Type { get; }

        [CanBeNull]
        public IAsi AffectedItem { get; set; }

        public String Text => AffectedItem == null
            ? Type.Template
            : getShortenedAsiText();


        private String getShortenedAsiText()
        {
            Contract.Requires(AffectedItem != null);

            var s = AffectedItem.Accept(Program.DefaultPrinter);
            var s2 = s.Length > 20 ? s.Substring(0, 50) + "..." : s;
            return String.Format(Type.Template, s2);
        }


        public Validation(ValidationType type, [CanBeNull] IAsi affectedItem)
        {
            Type = type;
            AffectedItem = affectedItem;
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
        private readonly List<Validation> validations = new List<Validation>();

        private readonly Dictionary<Int32, ValidationType> types;


        private ValidationList(Dictionary<Int32, ValidationType> ts)
        {
            types = ts;
        }


        public static ValidationList InitFrom(IEnumerable<String> lines)
        {
            var ts = new Dictionary<Int32, ValidationType>();
            foreach (var l in lines)
            {
                if (String.IsNullOrWhiteSpace(l))
                    continue;
                var split = l.Split('=');
                var cat = split[0][0];
                var number = split[0].Trim().Substring(1).ToInt();
                var template = split[1].Trim().Trim('"');
                var category = cat == 'p' ? ValidationCategory.Parsing : ValidationCategory.Runtime;
                ts.Add(number, new ValidationType(category, number, template));
            }
            return new ValidationList(ts);
        }


        private Validation add(Int32 number, IAsi affectedItem = null)
        {
            var v = new Validation(types[number], affectedItem);
            validations.Add(v);
            handle(v);
            return v;
        }


        private static void handle(Validation v)
        {
            Console.WriteLine(v.Text);
            if (v.Type.Severity == ValidationSeverity.Error)
                throw new ValidationException(v);
        }


        public Validation AddNothingAfterIf() => add(101);
        public Validation AddIfTestIsNotExp(IAsi affectedItem) => add(102, affectedItem);
    }
}