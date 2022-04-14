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
    /// <param name="initialCount">Total entities spawned in the initial appearance.</param>
    /// <returns>True if all <see cref="ecs"/> are generated from the <see cref="seed"/>.</returns>
    public static bool IsValidGroupSeed(in ulong seed, ReadOnlySpan<uint> ecs, in int firstIndexEC, int initialCount = 4)
    {
        if (ecs.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(ecs));
        if ((uint)firstIndexEC >= ecs.Length)
            throw new ArgumentOutOfRangeException(nameof(firstIndexEC));
        if ((uint)initialCount < ecs.Length)
            return false; // Can only handle initial spawns; we aren't permuting all sets of [2-initialCount] from the input ecs.

        int matched = 0;

        var rand = new Xoroshiro128Plus(seed);
        for (int count = 0; count < initialCount; count++)
        {
            var genseed = rand.Next();
            _ = rand.Next(); // unknown

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
    public static bool IsValidGroupSeedSingle(ulong seed, ReadOnlySpan<uint> ecs, in int firstIndexEC)
    {
        if (ecs.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(ecs));
        if ((uint)firstIndexEC >= ecs.Length)
            throw new ArgumentOutOfRangeException(nameof(ecs));

        var list = ecs.ToArray().ToList();

        while (list.Count != 0)
        {
            var rand = new Xoroshiro128Plus(seed);

            var genseed = rand.Next();
            _ = rand.Next(); // unknown

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
