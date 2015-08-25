using System;
using System.Collections.Generic;
using System.Linq;


namespace Efekt
{
    public sealed class Rewriter
    {
        public AsiList MakeProgram(IEnumerable<IAsi> prelude,
            Dictionary<String, IReadOnlyList<IAsi>> modules)
        {
            var mods = modules.Select(m => MakeModule(m.Key, m.Value));
            return new AsiList(prelude.Concat(mods).ToList());
        }


        public Declr MakeModule(String moduleName, IReadOnlyList<IAsi> moduleItems)
        {
            return new Declr(
                new Ident(moduleName, IdentCategory.Value),
                null,
                new New(new Struct(moduleItems))) { IsVar = true };
        }
    }
}