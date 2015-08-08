using System;
using System.Collections.Generic;
using System.Linq;


namespace Efekt
{
    public sealed class Interpreter : IAsiVisitor<Asi>
    {
        private Env env;


        public Asi VisitAsiList(AsiList al)
        {
            return visitAsiArray(al.Items);
        }


        public Asi VisitInt(Int ii)
        {
            return ii;
        }


        public Asi VisitIdent(Ident ident)
        {
            return env.GetValue(ident.Name);
        }


        public Asi VisitBinOpApply(BinOpApply opa)
        {
            switch (opa.Op.Name)
            {
                case "=":
                    var evaluated = opa.Op2.Accept(this);
                    var d = opa.Op1 as Declr;
                    Ident i;
                    if (d != null)
                    {
                        d.Accept(this);
                        i = d.Ident;
                    }
                    else
                    {
                        i = (Ident) opa.Op1;
                    }
                    env.SetValue(i.Name, evaluated);
                    return evaluated;
                default:
                    throw new NotSupportedException(
                        "operator '" + opa.Op.Name + "' is not supported");
            }
        }


        public Asi VisitDeclr(Declr d)
        {
            env.Delare(d.Ident.Name);
            return null;
        }


        public Asi VisitArr(Arr arr)
        {
            return arr;
        }


        public Asi VisitStruct(Struct s)
        {
            return s;
        }


        public Asi VisitFn(Fn fn)
        {
            return fn;
        }


        public Asi VisitFnApply(FnApply fna)
        {
            var fnAsi = fna.Fn.Accept(this);
            var fn = fnAsi as Fn;
            if (fn == null)
                throw new Exception("cannot apply " + fnAsi.GetType().Name);

            var preEnv = env;
            env = new Env(env);
            var n = 0;
            foreach (var p in fn.Params)
            {
                if (fna.Args.Count() <= n)
                {
                    p.Accept(this);
                    if (!(p is BinOpApply))
                        throw new Exception("fn has " + fn.Params.Count() + "parameters" +
                                            ", but calling with " + fna.Args.Count());
                }
                else
                {
                    var arg = fna.Args.ElementAt(n);
                    var argValue = arg.Accept(this);
                    var i = Parser.GetIdentFromDeclrLikeAsi(p);
                    if (p is BinOpApply)
                        env.Delare(i.Name);
                    else
                        p.Accept(this);
                    env.SetValue(i.Name, argValue);
                }
                ++n;
            }

            var res = visitAsiArray(fn.Items);

            env = preEnv;

            return res;
        }


        public Asi VisitNew(New n)
        {
            throw new NotImplementedException();
        }


        public Asi VisitVoid(Void v)
        {
            return v;
        }


        private Asi visitAsiArray(IEnumerable<Asi> items)
        {
            env = new Env(env);
            Asi res = null;
            foreach (var item in items)
            {
                res = item.Accept(this);
            }
            return res ?? new Void();
        }
    }
}