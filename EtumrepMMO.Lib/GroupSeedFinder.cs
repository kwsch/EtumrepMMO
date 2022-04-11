using PKHeX.Core;

namespace EtumrepMMO.Lib;

public static class GroupSeedFinder
{
    public const byte max_rolls = 32;

    #region Seed Detection

    /// <inheritdoc cref="FindSeed(IEnumerable{PKM},byte)"/>
    public static ulong FindSeed(string folder, byte maxRolls = max_rolls) => FindSeed(GetInputs(folder), maxRolls);

    /// <inheritdoc cref="FindSeed(IEnumerable{PKM},byte)"/>
    public static ulong FindSeed(IEnumerable<string> files, byte maxRolls = max_rolls) => FindSeed(GetInputs(files), maxRolls);

    /// <inheritdoc cref="FindSeed(IEnumerable{PKM},byte)"/>
    public static ulong FindSeed(IEnumerable<byte[]> data, byte maxRolls = max_rolls) => FindSeed(GetInputs(data), maxRolls);

    #endregion

    #region Data Fetching

    /// <summary> Gets entities from the provided input source. </summary>
    public static IReadOnlyList<PKM> GetInputs(string folder) => GetInputs(Directory.EnumerateFiles(folder));

    /// <inheritdoc cref="GetInputs(string)"/>
    public static IReadOnlyList<PKM> GetInputs(IEnumerable<string> files) => GetInputs(files.Select(File.ReadAllBytes));

    /// <inheritdoc cref="GetInputs(string)"/>
    public static IReadOnlyList<PKM> GetInputs(IEnumerable<byte[]> data) => data.Select(PKMConverter.GetPKMfromBytes).OfType<PKM>().ToArray();

    #endregion

    /// <summary>
    /// Returns all valid Group Seeds (should only be one) that generated the input data.
    /// </summary>
    /// <param name="data">Entities that were generated</param>
    /// <param name="maxRolls">Max amount of PID re-rolls for shiny odds.</param>
    /// <returns>Default if no result found, otherwise a single seed (no duplicates are possible).</returns>
    public static ulong FindSeed(IEnumerable<PKM> data, byte maxRolls = max_rolls)
    {
        var entities = data.ToArray();
        var ecs = Array.ConvertAll(entities, z => z.EncryptionConstant);

        // Backwards we go! Reverse the pkm data -> seed first (this takes the longest, so we only do one at a time).
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            Console.WriteLine($"Checking entity {i+1}/{entities.Length} for group seeds...");
            var pokeResult = RuntimeReversal.GetSeeds(entity, maxRolls);

            foreach (var (pokeSeed, rolls) in pokeResult)
            {
                // Get seed for slot-pkm
                var genSeeds = GenSeedReversal.FindPotentialGenSeeds(pokeSeed);
                foreach (var genSeed in genSeeds)
                {
                    // Get the group seed - O(1) calc
                    var groupSeed = GroupSeedReversal.GetGroupSeed(genSeed);
                    if (!IsValidGroupSeed(groupSeed, ecs))
                        continue;

                    Console.WriteLine($"Found a group seed with PID roll count = {rolls}");
                    return groupSeed;
                }
            }
        }
        return default;
    }

    /// <summary>
    /// Uses the input <see cref="seed"/> as the group seed to check if it generates all of the input <see cref="PKM.EncryptionConstant"/> values.
    /// </summary>
    /// <param name="seed">Group seed</param>
    /// <param name="ecs">Entity encryption constants</param>
    /// <returns>True if all <see cref="ecs"/> are generated from the <see cref="seed"/>.</returns>
    private static bool IsValidGroupSeed(ulong seed, ReadOnlySpan<uint> ecs)
    {
        int matched = 0;

        var rng = new Xoroshiro128Plus(seed);
        for (int count = 0; count < 4; count++)
        {
            var genseed = rng.Next();
            _ = rng.Next(); // unknown

            var slotrng = new Xoroshiro128Plus(genseed);
            _ = slotrng.Next(); // slot
            var mon_seed = slotrng.Next();
         // _ = slotrng.Next(); // level

            var monrng = new Xoroshiro128Plus(mon_seed);
            var ec = (uint)monrng.NextInt();

            var index = ecs.IndexOf(ec);
            if (index != -1)
                matched++;
        }

        return matched == ecs.Length;
    }
}
