using PKHeX.Core;

namespace EtumrepMMO.Lib;

/// <summary>
/// Reverses for top step seeds by using basic arithmetic.
/// </summary>
public static class GroupSeedReversal
{
    /// <summary>
    /// Top level seed calculation for the initial Group Seed
    /// </summary>
    /// <param name="genSeed">Middle level Generator seed that generates Slot and the Entity seed.</param>
    public static ulong GetGroupSeed(ulong genSeed)
    {
        // forward operation is just new(groupSeed).Next(), which is just result=(s0+s1).
        // s1 is const, so just O(1) arithmetic :)
        return unchecked(genSeed - Xoroshiro128Plus.XOROSHIRO_CONST);
    }
}
