using System;
using System.Collections.Generic;


namespace Efekt
{
    public sealed class SimpleEnv
    {
        Dictionary<String, IAsi> dict;

        public SimpleEnv Parent { get; set; }


        public SimpleEnv()
        {
        }


        public SimpleEnv(SimpleEnv parent)
        {
            Parent = parent;
        }


        public IAsi GetValueOrNull(String name)
        {
            var e = this;
            do
            {
                if (dict.ContainsKey(name))
                    return dict[name];
                e = e.Parent;
            } while (e != null);
            return null;
        }


        public void Declare(String name, IAsi value)
        {
            dict.Add(name, value);
        }
    }
}