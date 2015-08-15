using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Env
    {
        [CanBeNull]
        public Env Parent { get; }

        public Dictionary<String, IAsi> Dict { get; } = new Dictionary<String, IAsi>();

        public List<Env> ImportedEnvs { get; set; } = new List<Env>();


        public Env()
        {
        }


        public Env(Env parent)
        {
            Contract.Requires(parent != null);
            Parent = parent;
        }


        Env(Dictionary<String, IAsi> dictionary)
        {
            Dict = dictionary;
        }


        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public Env GetFlat()
        {
            var res = new Env();
            var imports = new List<Env>();
            var e = this;
            do
            {
                foreach (var kvp in e.Dict)
                    if (!res.Dict.ContainsKey(kvp.Key))
                        res.Dict.Add(kvp.Key, kvp.Value);
                imports.AddRange(Enumerable.Reverse(e.ImportedEnvs));
                e = e.Parent;
            } while (e != null);
            foreach (var i in imports)
                if (!res.ImportedEnvs.Contains(i))
                    res.ImportedEnvs.Add(i);
            return res;
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


        public void Declare(String name)
        {
            if (Dict.ContainsKey(name))
                throw new EfektException("variable '" + name + "' is already declared");
            Dict.Add(name, null);
        }


        public void SetValue(String name, IAsi value)
            => getEnvDeclaring(name, this).Dict[name] = value;


        public void AddImport(Env ie) => ImportedEnvs.Insert(0, new Env(ie.Dict));

        public IAsi GetValue(String name) => getEnvDeclaring(name, this).Dict[name];

        public IAsi GetValueOrNull(String name) => getEnvDeclaringOrNull(name, this)?.Dict[name];


        public static void PrintEnv(Env env)
        {
            Console.WriteLine("Env:");
            var e = env;
            do
            {
                foreach (var d in e.Dict)
                    Console.WriteLine("  var " + d.Key + " = " +
                                      d.Value.Accept(Program.DefaultPrinter));
                e = env.Parent;
            } while (e != null);
        }


        Env getEnvDeclaring(String name, Env env)
        {
            var e = getEnvDeclaringOrNull(name, env);
            if (e == null)
                throw new EfektException("variable '" + name + "' is not declared");
            return e;
        }


        [CanBeNull]
        Env getEnvDeclaringOrNull(String name, Env env)
        {
            if (env.Dict.ContainsKey(name))
                return env;
            if (env.ImportedEnvs.Count != 0)
            {
                foreach (var ie in env.ImportedEnvs)
                {
                    if (ie.Dict.ContainsKey(name))
                        return env;
                }
                return null;
            }
            return env.Parent == null ? null : getEnvDeclaringOrNull(name, env.Parent);
        }
    }
}