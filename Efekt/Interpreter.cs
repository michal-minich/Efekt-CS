using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;


namespace Efekt
{
    public sealed class Interpreter : IAsiVisitor<Asi>
    {
        private Env env;


        public Asi VisitAsiList(AsiList al) => visitAsiArray(al.Items, new Env(env), env);


        public Asi VisitInt(Int ii) => ii;


        public Asi VisitIdent(Ident ident) => env.GetValue(ident.Name);


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
                case "+":
                    var v1 = opa.Op1.Accept(this).Accept(Program.DefaultPrinter).ToInt();
                    var v2 = opa.Op2.Accept(this).Accept(Program.DefaultPrinter).ToInt();
                    return new Int((v1 + v2).ToString());
                default:
                    var fna = new FnApply(opa.Op, new[] {opa.Op1, opa.Op2});
                    return VisitFnApply(fna);
            }
        }


        public Asi VisitDeclr(Declr d)
        {
            env.Declare(d.Ident.Name);
            return null;
        }


        public Asi VisitArr(Arr arr) => arr;


        public Asi VisitStruct(Struct s) => s;


        public Asi VisitFn(Fn fn)
        {
            Contract.Assume(fn.Env == null);
            return new Fn(fn.Params, fn.Items) {Env = env.GetFlat()};
        }


        public Asi VisitFnApply(FnApply fna)
        {
            var fnAsi = fna.Fn.Accept(this);
            var fn = fnAsi as Fn;
            if (fn == null)
                throw new EfektException("cannot apply " + fnAsi.GetType().Name);

            Contract.Assume(fn.Env != null);

            var prevEnv = env;
            env = new Env(env); // for params
            var n = 0;
            foreach (var p in fn.Params)
            {
                var opa = p as BinOpApply;
                if (fna.Args.Count() <= n)
                {
                    p.Accept(this);
                    if (opa == null)
                        throw new EfektException("fn has " + fn.Params.Count() + " parameter(s)" +
                                                 ", but calling with " + fna.Args.Count());
                }
                else
                {
                    var arg = fna.Args.ElementAt(n);
                    var argValue = arg.Accept(this);
                    var i = Parser.GetIdentFromDeclrLikeAsi(p);
                    if (opa != null)
                        env.Declare(i.Name);
                    else
                        p.Accept(this);
                    env.SetValue(i.Name, argValue);
                }
                ++n;
            }
            var localEnv = new Env(env); // new local env for this fn call
            localEnv.CopyFrom(env); // make params local variables of fn
            localEnv.Parent = fn.Env; // reference captured env
            var res = visitAsiArray(fn.Items, localEnv, prevEnv);
            return res;
        }


        public Asi VisitNew(New n)
        {
            throw new NotImplementedException();
        }


        public Asi VisitVoid(Void v) => v;


        private Asi visitAsiArray(IEnumerable<Asi> items, Env newEnv, Env restoreEnv)
        {
            env = newEnv;
            Asi res = null;
            foreach (var item in items)
                res = item.Accept(this);
            env = restoreEnv;
            return res ?? new Void();
        }
    }
}