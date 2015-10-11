using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using JetBrains.Annotations;


namespace Efekt
{
    public enum Accessibility
    {
        None,
        Local,
        Private,
        Public,
        Global
    }


    public sealed class EnvItem
    {
        public Accessibility Accessibility { get; }

        public Exp Item { get; }


        public EnvItem(Accessibility accessibility, Exp item)
        {
            Accessibility = accessibility;
            Item = item;
        }


        public override String ToString() => Accessibility + " " + Item;
    }


    public sealed class Env
    {
        readonly ValidationList validations;
        public Class Owner { get; }

        [CanBeNull]
        Env parent { get; }

        readonly Dictionary<String, EnvItem> dict = new Dictionary<String, EnvItem>();

        readonly List<Env> imports = new List<Env>();


        public Env(ValidationList validations, Class owner)
        {
            Contract.Requires(validations != null);

            this.validations = validations;
            Owner = owner;
        }


        public Env(ValidationList validations, Class owner, Env parent)
        {
            Contract.Requires(validations != null);
            Contract.Requires(parent != null);

            this.validations = validations;
            Owner = owner;
            this.parent = parent;
        }


        Env(ValidationList validations, Dictionary<String, EnvItem> dictionary)
        {
            Contract.Requires(validations != null);

            this.validations = validations;
            dict = dictionary;
        }


        public void Declare(Accessibility accessibility, String name, Exp value = null)
        {
            if (dict.ContainsKey(name))
                validations.GenericWarning("variable '" + name + "' is already declared",
                                           Void.Instance);
            dict.Add(name, new EnvItem(accessibility, value));
        }


        public void SetValue(String name, Exp value)
        {
            var e = getEnvDeclaring(name, this);
            CheckAccessibility(name, e, "write");
            e.dict[name] = new EnvItem(e.dict[name].Accessibility, value);
        }


        public void AddImport(Env ie) => imports.Insert(0, new Env(validations, ie.dict));


        public IAsi GetValueOrNull(String name)
        {
            var e = getEnvDeclaringOrNull(name, this);
            var i = CheckAccessibility(name, e, "read");
            return i;
        }


        public IAsi CheckAccessibility(String name, Env e, String accessType)
        {
            if (e == null)
                return null;
            if (e == this)
                return e.dict[name].Item;
            var i = e.dict[name];
            if (i.Accessibility == Accessibility.Private)
                validations.GenericWarning(
                    "Cannot " + accessType + " private variable '" + name + "' from here.",
                    Void.Instance);
            return i.Item;
        }


        public IAsi GetOwnValueOrNull(String name)
        {
            if (dict.ContainsKey(name))
            {
                var i = dict[name];
                if (i.Accessibility == Accessibility.Private)
                    validations.GenericWarning(
                        "Cannot read private member variable '" + name + "' from here.",
                        Void.Instance);
                return i.Item;
            }
            return null;
        }


        public static void PrintEnv(Env env)
        {
            Console.WriteLine("Env:");
            var e = env;
            var indent = "";
            do
            {
                foreach (var d in e.dict)
                {
                    Console.WriteLine(indent + d.Value.Accessibility + " var " + d.Key + " = " +
                                      d.Value.Item.Accept(Program.DefaultPrinter));
                }
                indent += "  ";
                e = e.parent;
            } while (e != null);
        }


        Env getEnvDeclaring(String name, Env env)
        {
            var e = getEnvDeclaringOrNull(name, env);
            if (e == null)
                validations.GenericWarning("Env: variable '" + name + "' is not declared",
                                           Void.Instance);
            return e;
        }


        [CanBeNull]
        static Env getEnvDeclaringOrNull(String name, Env env)
        {
            if (env.dict.ContainsKey(name))
                return env;
            if (env.imports.Count != 0)
            {
                foreach (var ie in env.imports)
                {
                    if (ie.dict.ContainsKey(name))
                        return ie;
                }
                return null;
            }
            return env.parent == null ? null : getEnvDeclaringOrNull(name, env.parent);
        }
    }
}