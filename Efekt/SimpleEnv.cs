using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;


namespace Efekt
{
    public sealed class SimpleEnv<T> where T : class
    {
        [CanBeNull] readonly SimpleEnv<T> parent;
        readonly Dictionary<String, T> dict = new Dictionary<String, T>();
        readonly Dictionary<String, SimpleEnv<T>> imports = new Dictionary<String, SimpleEnv<T>>();


        public SimpleEnv()
        {
        }


        public SimpleEnv(SimpleEnv<T> parent)
        {
            this.parent = parent;
        }


        public T GetValueOrNull(String name)
        {
            var e = this;
            do
            {
                if (e.dict.ContainsKey(name))
                    return e.dict[name];
                var impValue = getValueOrNullFromImport(name, e.imports);
                if (impValue != null)
                    return impValue;
                e = e.parent;
            } while (e != null);

            return null;
        }


        static T getValueOrNullFromImport(String name, Dictionary<String, SimpleEnv<T>> importDict)
        {
            foreach (var kvp in importDict)
            {
                if (kvp.Value.dict.ContainsKey(name))
                    return kvp.Value.dict[name];
            }
            return null;
        }


        public void Declare(String name, T value) => dict.Add(name, value);


        public void AddImport(String name, SimpleEnv<T> impEnv) => imports.Add(name, impEnv);
    }
}