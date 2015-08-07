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


        public void Delare(String name)
        {
            if (dict.ContainsKey(name))
                throw new Exception("variable '" + name + "' is already declared");
            dict.Add(name, null);
        }


        public void SetValue(String name, Asi value)
        {
            getEnvDeclaring(name).dict[name] = value;
        }


        public Asi GetValue(String name)
        {
            return getEnvDeclaring(name).dict[name];
        }


        private Env getEnvDeclaring(String name)
        {
            if (dict.ContainsKey(name))
                return this;
            if (Parent != null)
                return Parent;
            throw new Exception("variable '" + name + "' is not declared");
        }
    }
}