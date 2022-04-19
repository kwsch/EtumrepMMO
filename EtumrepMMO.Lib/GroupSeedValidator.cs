using PKHeX.Core;

namespace EtumrepMMO.Lib;

/// <summary>
/// Methods for validating an input group seed.
/// </summary>
public static class GroupSeedValidator
{
    /// <summary>
    /// Uses the input <see cref="seed"/> as the group seed to check if it generates all of the input <see cref="PKM.EncryptionConstant"/> values.
    /// </summary>
    /// <param name="seed">Group seed</param>
    /// <param name="ecs">Entity encryption constants</param>
    /// <param name="firstIndexEC">Initial spawn EC index</param>
    /// <param name="count">Count of entities spawned in the initial appearance.</param>
    /// <returns>True if all <see cref="ecs"/> are generated from the <see cref="seed"/>.</returns>
    /// <remarks>Pattern: Multi spawner spawns count >=2, and only count.</remarks>
    public static bool IsMultiInitial(in ulong seed, ReadOnlySpan<uint> ecs, in int firstIndexEC, int count = 4)
    {
        if (ecs.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(ecs));
        if ((uint)firstIndexEC >= ecs.Length)
            throw new ArgumentOutOfRangeException(nameof(firstIndexEC));
        if (count == 1 || ecs.Length == 1)
            return false; // We're expecting a multi-spawn initial; the inputs obviously aren't.
        if ((uint)count < ecs.Length)
            return false; // Can only handle initial spawns; we aren't permuting all sets of [2-initialCount] from the input ecs.

        int matched = 0;

        var rand = new Xoroshiro128Plus(seed);
        for (int i = 0; i < count; i++)
        {
            var genseed = rand.Next(); // generate/slot seed
            _ = rand.Next(); // alpha move, don't care

            var ec = GetEncryptionConstant(genseed);
            var index = ecs.IndexOf(ec);
            if (index != -1)
                matched++;
        }

        return matched == ecs.Length;
    }

    /// <summary>
    /// Uses the input <see cref="seed"/> as the group seed to check if it generates all of the input <see cref="PKM.EncryptionConstant"/> values.
    /// </summary>
    /// <param name="seed">Group seed</param>
    /// <param name="ecs">Entity encryption constants</param>
    /// <param name="firstIndexEC">Initial spawn EC index</param>
    /// <returns>True if all <see cref="ecs"/> are generated from the <see cref="seed"/>.</returns>
    /// <remarks>Pattern: Single spawner spawns 1, then 1 for each successive wave.</remarks>
    public static bool IsSingleSingle(ulong seed, ReadOnlySpan<uint> ecs, in int firstIndexEC)
    {
        if (ecs.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(ecs));
        if ((uint)firstIndexEC >= ecs.Length)
            throw new ArgumentOutOfRangeException(nameof(ecs));
        if (ecs.Length == 1)
            return false;

        var list = new List<uint>(ecs.ToArray());

        while (list.Count != 0)
        {
            var rand = new Xoroshiro128Plus(seed);

            var genseed = rand.Next(); // generate/slot seed
            _ = rand.Next(); // alpha move, don't care

            var ec = GetEncryptionConstant(genseed);
            var index = list.IndexOf(ec);
            if (index == -1)
                return false;
            if (list.Count == ecs.Length && ec != ecs[firstIndexEC])
                return false;
            list.RemoveAt(index);

            seed = rand.Next();
        }

        return true;
    }

    /// <summary>
    /// Uses the input <see cref="seed"/> as the group seed to check if it generates all of the input <see cref="PKM.EncryptionConstant"/> values.
    /// </summary>
    /// <param name="seed">Group seed</param>
    /// <param name="ecs">Entity encryption constants</param>
    /// <param name="firstIndexEC">Initial spawn EC index</param>
    /// <returns>True if all <see cref="ecs"/> are generated from the <see cref="seed"/>.</returns>
    /// <remarks>Pattern: Multi spawner spawns 1, then >1 for the next wave.</remarks>
    public static bool IsSingleMulti(ulong seed, ReadOnlySpan<uint> ecs, in int firstIndexEC)
    {
        if (ecs.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(ecs));
        if ((uint)firstIndexEC >= ecs.Length)
            throw new ArgumentOutOfRangeException(nameof(ecs));
        if (ecs.Length == 1)
            return false;

        var list = new List<uint>(ecs.ToArray());

        // Check first
        var rand = new Xoroshiro128Plus(seed);
        {
            var genseed = rand.Next(); // generate/slot seed
            _ = rand.Next(); // alpha move, don't care
            var ec = GetEncryptionConstant(genseed);
            if (ec != ecs[firstIndexEC])
                return false;

            var index = list.IndexOf(ec);
            if (index == -1)
                return false;

            list.RemoveAt(index);
        }
        seed = rand.Next();

        rand = new Xoroshiro128Plus(seed);
        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            var genseed = rand.Next(); // generate/slot seed
            _ = rand.Next(); // alpha move, don't care

            var ec = GetEncryptionConstant(genseed);
            var index = list.IndexOf(ec);
            if (index == -1)
                return false;

            list.RemoveAt(index);
        }

        return true;
    }

    private static uint GetEncryptionConstant(ulong genSeed)
    {
        var slotRand = new Xoroshiro128Plus(genSeed);
        _ = slotRand.Next(); // slot
        var entitySeed = slotRand.Next();
     // _ = slotrng.Next(); // level

        var entityRand = new Xoroshiro128Plus(entitySeed);
        return (uint)entityRand.NextInt();
    }
}
