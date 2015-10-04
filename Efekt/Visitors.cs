using System.Diagnostics.Contracts;


namespace Efekt
{
    [ContractClass(typeof (IAsiVisitorContract<>))]
    public interface IAsiVisitor<out T> where T : class
    {
        T VisitAsiList(AsiList al);
        T VisitErr(Err err);
        T VisitInt(Int ii);
        T VisitIdent(Ident i);
        T VisitBinOpApply(BinOpApply opa);
        T VisitDeclr(Declr d);
        T VisitArr(Arr arr);
        T VisitStruct(Struct s);
        T VisitClass(Class cls);
        T VisitFn(Fn fn);
        T VisitFnApply(FnApply fna);
        T VisitNew(New n);
        T VisitVoid(Void v);
        T VisitBool(Bool b);
        T VisitChar(Char c);
        T VisitIf(If iff);
        T VisitImport(Import imp);
        T VisitAssign(Assign a);
        T VisitGoto(Goto gt);
        T VisitLabel(Label lbl);
        T VisitBreak(Break br);
        T VisitContinue(Continue ct);
        T VisitReturn(Return r);
        T VisitRepeat(Repeat rp);
        T VisitForEach(ForEach fe);
        T VisitThrow(Throw th);
        T VisitTry(Try tr);
        T VisitAssume(Assume asm);
        T VisitAssert(Assert ast);
        T VisitSimpleType(ISimpleType st);
    }


    [ContractClassFor(typeof (IAsiVisitor<>))]
    abstract class IAsiVisitorContract<T> : IAsiVisitor<T> where T : class
    {
        T IAsiVisitor<T>.VisitAsiList(AsiList al)
        {
            Contract.Requires(al != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitErr(Err err)
        {
            Contract.Requires(err != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitInt(Int ii)
        {
            Contract.Requires(ii != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitIdent(Ident i)
        {
            Contract.Requires(i != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitBinOpApply(BinOpApply opa)
        {
            Contract.Requires(opa != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitDeclr(Declr d)
        {
            Contract.Requires(d != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitArr(Arr arr)
        {
            Contract.Requires(arr != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitStruct(Struct s)
        {
            Contract.Requires(s != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitClass(Class cls)
        {
            Contract.Requires(cls != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitFn(Fn fn)
        {
            Contract.Requires(fn != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitFnApply(FnApply fna)
        {
            Contract.Requires(fna != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitNew(New n)
        {
            Contract.Requires(n != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitVoid(Void v)
        {
            Contract.Requires(v != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitBool(Bool b)
        {
            Contract.Requires(b != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitChar(Char c)
        {
            Contract.Requires(c != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitIf(If iff)
        {
            Contract.Requires(iff != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitImport(Import imp)
        {
            Contract.Requires(imp != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        T IAsiVisitor<T>.VisitAssign(Assign a)
        {
            Contract.Requires(a != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitGoto(Goto gt)
        {
            Contract.Requires(gt != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitLabel(Label lbl)
        {
            Contract.Requires(lbl != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitBreak(Break br)
        {
            Contract.Requires(br != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitContinue(Continue ct)
        {
            Contract.Requires(ct != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitReturn(Return r)
        {
            Contract.Requires(r != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitRepeat(Repeat rp)
        {
            Contract.Requires(rp != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitForEach(ForEach fe)
        {
            Contract.Requires(fe != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitThrow(Throw th)
        {
            Contract.Requires(th != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitTry(Try tr)
        {
            Contract.Requires(tr != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitAssume(Assume asm)
        {
            Contract.Requires(asm != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitAssert(Assert ast)
        {
            Contract.Requires(ast != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }


        public T VisitSimpleType(ISimpleType st)
        {
            Contract.Requires(st != null);
            Contract.Ensures(Contract.Result<T>() != null);
            return null;
        }
    }
}