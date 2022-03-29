using Microsoft.Z3;
using PKHeX.Core;

namespace EtumrepMMO.Lib;

/// <summary>
/// Reverses for middle step seeds by using Z3 to calculate.
/// </summary>
public static class GenSeedReversal
{
    /// <summary>
    /// Middle level seed calculation for the Generator Seed
    /// </summary>
    /// <param name="seed">Bottom level Entity seed.</param>
    public static IEnumerable<ulong> FindPotentialGenSeeds(ulong seed)
    {
        using var ctx = new Context(new Dictionary<string, string> { { "model", "true" } });
        var exp = CreateGenSeedModel(ctx, seed, out var s0);

        return FindAllMatches(ctx, exp, s0);
    }

    private static IEnumerable<ulong> FindAllMatches(Context ctx, BoolExpr exp, BitVecExpr s0)
    {
        while (Check(ctx, exp) is { } m)
        {
            foreach (var kvp in m.Consts)
            {
                var tmp = (BitVecNum)kvp.Value;
                yield return tmp.UInt64;
                exp = ctx.MkAnd(exp, ctx.MkNot(ctx.MkEq(s0, m.Evaluate(s0))));
            }
        }
    }

    private static BoolExpr CreateGenSeedModel(Context ctx, ulong seed, out BitVecExpr s0)
    {
        s0 = ctx.MkBVConst("s0", 64);
        BitVecExpr s1 = ctx.MkBV(Xoroshiro128Plus.XOROSHIRO_CONST, 64);

        var real_seed = ctx.MkBV(seed, 64);

        AdvanceSymbolicNext(ctx, ref s0, ref s1); // slot
        var genseed_check = AdvanceSymbolicNext(ctx, ref s0, ref s1); // genseed

        return ctx.MkEq(real_seed, genseed_check);
    }

    private static BitVecExpr AdvanceSymbolicNext(Context ctx, ref BitVecExpr s0, ref BitVecExpr s1)
    {
        var and_val = ctx.MkBV(0xFFFFFFFFFFFFFFFF, 64);
        var res = ctx.MkBVAND(ctx.MkBVAdd(s0, s1), and_val);
        s1 = ctx.MkBVXOR(s0, s1);
        var tmp = ctx.MkBVRotateLeft(24, s0);
        var tmp2 = ctx.MkBV(1 << 16, 64);
        s0 = ctx.MkBVXOR(tmp, ctx.MkBVXOR(s1, ctx.MkBVMul(s1, tmp2)));
        s1 = ctx.MkBVRotateLeft(37, s1);
        return res;
    }

    private static Model? Check(Context ctx, BoolExpr cond)
    {
        Solver solver = ctx.MkSolver();
        solver.Assert(cond);
        Status q = solver.Check();
        if (q != Status.SATISFIABLE)
            return null;
        return solver.Model;
    }
}
