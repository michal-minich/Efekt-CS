using System;
using System.Collections.Generic;
using System.Linq;


namespace Efekt
{
    public class Prog
    {
        public IReadOnlyList<Declr> Modules { get; set; }


        public Prog(IReadOnlyList<Declr> modules)
        {
            Modules = modules;
        }
    }


    public sealed class Rewriter
    {
        public Prog MakeProgram(IReadOnlyList<IAsi> prelude,
                                Dictionary<String, IReadOnlyList<IAsi>> modules)
        {
            var p = MakeModule("prelude", prelude);
            var mods = modules.Select(m => MakeModule(m.Key, m.Value));
            return new Prog(p.Append(mods).ToList());
        }


        public Declr MakeModule(String moduleName, IReadOnlyList<IAsi> moduleItems)
        {
            IReadOnlyList<IAsi> modItems;
            if (moduleName != "prelude")
                modItems = new Import
                {
                    QualifiedIdent = new Ident("prelude", IdentCategory.Value)
                }.Append(moduleItems).ToList();
            else
                modItems = moduleItems;

            return new Declr(
                new Ident(moduleName, IdentCategory.Value),
                null,
                new New(new Struct(modItems)))
            {
                IsVar = true,
                Attributes = new List<IExp> { new Ident("public") }
            };
        }
    }
}