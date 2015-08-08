using System;
using System.Collections.Generic;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class Env
    {
        [CanBeNull]
        public Env Parent { get; }

        private readonly Dictionary<String, Asi> dict = new Dictionary<String, Asi>();


        public Env([CanBeNull] Env parent)
        {
            Parent = parent;
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


        public Asi GetValue(String name)
        {
            return getEnvDeclaring(name, this).dict[name];
        }


        private Env getEnvDeclaring(String name, Env env)
        {
            if (env.dict.ContainsKey(name))
                return env;
            if (env.Parent != null)
                return getEnvDeclaring(name, env.Parent);
            throw new EfektException("variable '" + name + "' is not declared");
        }
    }
}