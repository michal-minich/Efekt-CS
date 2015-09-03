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

    public sealed class Env
    {
        public sealed class EnvItem
        {
            public Accessibility Accessibility { get; }

            public IAsi Item { get; }


            public EnvItem(Accessibility accessibility, IAsi item)
            {
                Accessibility = accessibility;
                Item = item;
            }


            public override String ToString() => Accessibility + " " + Item;
        }


        readonly ValidationList validations;
        public Struct Owner { get; }

        [CanBeNull]
        public Env Parent { get; }

        public Dictionary<String, EnvItem> Dict { get; } = new Dictionary<String, EnvItem>();

        public List<Env> ImportedEnvs { get; set; } = new List<Env>();


        public Env(ValidationList validations, Struct owner)
        {
            this.validations = validations;
            Owner = owner;
        }


        public Env(ValidationList validations, Struct owner, Env parent)
        {
            Contract.Requires(parent != null);

            this.validations = validations;
            Owner = owner;
            Parent = parent;
        }


        Env(Dictionary<String, EnvItem> dictionary)
        {
            Dict = dictionary;
        }


        public void CopyFrom(Env env)
        {
            ImportedEnvs = env.ImportedEnvs;
            foreach (var kvp in env.Dict)
                if (Dict.ContainsKey(kvp.Key))
                    Dict[kvp.Key] = kvp.Value;
                else
                    Dict.Add(kvp.Key, kvp.Value);
        }


        public void Declare(Accessibility accessibility, String name, IAsi value = null)
        {
            if (Dict.ContainsKey(name))
                validations.GenericWarning("variable '" + name + "' is already declared",
                    Void.Instance);
            Dict.Add(name, new EnvItem(accessibility, value));
        }


        public void SetValue(String name, IAsi value)
        {
            var e = getEnvDeclaring(name, this);
            CheckAccessibility(name, e, "write");
            e.Dict[name] = new EnvItem(e.Dict[name].Accessibility, value);
        }


        public void AddImport(Env ie) => ImportedEnvs.Insert(0, new Env(ie.Dict));


        public IAsi GetValueOrNull(String name)
        {
            var e = getEnvDeclaringOrNull(name, this);
            var i = CheckAccessibility(name, e, "read");
            return i;
        }


        public IAsi CheckAccessibility(String name, Env e, string accessType)
        {
            if (e == null)
                return null;
            if (e == this)
                return e.Dict[name].Item;
            var i = e.Dict[name];
            if (i.Accessibility == Accessibility.Private)
                validations.GenericWarning(
                    "Cannot " + accessType + " private variable '" + name + "' from here.",
                    Void.Instance);
            return i.Item;
        }


        public IAsi GetOwnValueOrNull(String name)
        {
            if (Dict.ContainsKey(name))
            {
                var i = Dict[name];
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
                foreach (var d in e.Dict)
                {
                    Console.WriteLine(indent + d.Value.Accessibility + " var " + d.Key + " = " +
                                      d.Value.Item.Accept(Program.DefaultPrinter));
                }
                indent += "  ";
                e = e.Parent;
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
            if (env.Dict.ContainsKey(name))
                return env;
            if (env.ImportedEnvs.Count != 0)
            {
                foreach (var ie in env.ImportedEnvs)
                {
                    if (ie.Dict.ContainsKey(name))
                        return ie;
                }
                return null;
            }
            return env.Parent == null ? null : getEnvDeclaringOrNull(name, env.Parent);
        }
    }
}