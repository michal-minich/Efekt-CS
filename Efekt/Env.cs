using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Env
    {
        [CanBeNull]
        public Env Parent { get; set; }

        private readonly Dictionary<String, Asi> dict = new Dictionary<String, Asi>();


        public Env([CanBeNull] Env parent)
        {
            Parent = parent;
        }


        private Env(Dictionary<String, Asi> dictionary)
        {
            dict = dictionary;
        }


        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public Env GetFlat()
        {
            var d = new Dictionary<String, Asi>();
            var e = this;
            do
            {
                foreach (var kvp in e.dict)
                    if (!d.ContainsKey(kvp.Key))
                        d.Add(kvp.Key, kvp.Value);
                e = e.Parent;
            } while (e != null);
            return new Env(d);
        }


        public void CopyFrom(Env env)
        {
            foreach (var kvp in env.dict)
                if (dict.ContainsKey(kvp.Key))
                    dict[kvp.Key] = kvp.Value;
                else
                    dict.Add(kvp.Key, kvp.Value);
        }


        public void Declare(String name)
        {
            if (dict.ContainsKey(name))
                throw new EfektException("variable '" + name + "' is already declared");
            dict.Add(name, null);
        }


        public void SetValue(String name, Asi value)
        {
            getEnvDeclaring(name, this).dict[name] = value;
        }


        public Asi GetValue(String name) => getEnvDeclaring(name, this).dict[name];


        public static void PrintEnv(Env env)
        {
            Console.WriteLine("Env:");
            var e = env;
            do
            {
                foreach (var d in e.dict)
                    Console.WriteLine("  var " + d.Key + " = " +
                                      d.Value.Accept(Program.DefaultPrinter));
                e = env.Parent;
            } while (e != null);
        }


        private Int32 n;


        private Env getEnvDeclaring(String name, Env env)
        {
            if (++n == 20)
            {
                n = 0;
                throw new EfektException("too many nested environments?");
            }
            if (env.dict.ContainsKey(name))
                return env;
            if (env.Parent != null)
                return getEnvDeclaring(name, env.Parent);
            throw new EfektException("variable '" + name + "' is not declared");
        }
    }
}