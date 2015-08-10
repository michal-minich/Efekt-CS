﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Env
    {
        [CanBeNull]
        public Env Parent { get; set; }

        public Dictionary<String, Asi> Dict { get; } = new Dictionary<String, Asi>();


        public Env([CanBeNull] Env parent)
        {
            Parent = parent;
        }


        private Env(Dictionary<String, Asi> dictionary)
        {
            Dict = dictionary;
        }


        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public Env GetFlat()
        {
            var d = new Dictionary<String, Asi>();
            var e = this;
            do
            {
                foreach (var kvp in e.Dict)
                    if (!d.ContainsKey(kvp.Key))
                        d.Add(kvp.Key, kvp.Value);
                e = e.Parent;
            } while (e != null);
            return new Env(d);
        }


        public void CopyFrom(Env env)
        {
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


        public void SetValue(String name, Asi value)
        {
            getEnvDeclaring(name, this).Dict[name] = value;
        }


        public Asi GetValue(String name) => getEnvDeclaring(name, this).Dict[name];

        public Asi GetValueOrNull(String name) => getEnvDeclaringOrNull(name, this)?.Dict[name];


        public Boolean IsDeclared(String name) => getEnvDeclaringOrNull(name, this) != null;


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


        [CanBeNull]
        private Env getEnvDeclaringOrNull(String name, Env env)
        {
            var e = getEnvDeclaring2(name, env);
            n = 0;
            return e;
        }


        private Int32 n;


        private Env getEnvDeclaring(String name, Env env)
        {
            var e = getEnvDeclaring2(name, env);
            if (e == null)
                throw new EfektException("variable '" + name + "' is not declared");
            n = 0;
            return e;
        }


        [CanBeNull]
        private Env getEnvDeclaring2(String name, Env env)
        {
            if (++n == 10)
            {
                n = 0;
                throw new EfektException("too many nested environments?");
            }
            if (env.Dict.ContainsKey(name))
                return env;
            if (env.Parent != null)
                return getEnvDeclaring2(name, env.Parent);
            return null;
        }
    }
}