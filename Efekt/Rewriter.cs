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
        public Prog MakeProgram(IReadOnlyList<Declr> prelude,
                                Dictionary<String, IReadOnlyList<IClassItem>> modules)
        {
            var p = MakeModule("prelude", prelude);
            var mods = modules.Select(m => MakeModule(m.Key, m.Value));
            return new Prog(p.Append(mods).ToList());
        }


        public Declr MakeModule(String moduleName, IReadOnlyList<IClassItem> moduleItems)
        {
            IReadOnlyList<IClassItem> modItems;
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
                new New(new Class(modItems)))
            {
                IsVar = true,
                Attributes = new List<Exp> { new Ident("public") }
            };
        }
    }
}