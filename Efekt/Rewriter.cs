using System;
using System.Collections.Generic;
using System.Linq;


namespace Efekt
{
    public class Prog
    {
        public Declr GlobalModule { get; }


        public Prog(Declr globalModule)
        {
            GlobalModule = globalModule;
        }
    }


    public sealed class Rewriter
    {
        public Prog MakeProgram(IReadOnlyList<Declr> prelude,
                                Dictionary<String, IReadOnlyList<IClassItem>> modules)
        {
            var p = MakeModule("prelude", prelude, false);
            var mods = modules.Select(m => MakeModule(m.Key, m.Value));
            return new Prog(MakeModule("global", p.Append(mods).ToList(), false));
        }


        public Declr MakeModule(String moduleName, IReadOnlyList<IClassItem> moduleItems, bool importPrelude = true)
        {
            IReadOnlyList<IClassItem> modItems;
            if (importPrelude)
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
                Attributes = new List<Ident> { new Ident("public") }
            };
        }
    }
}