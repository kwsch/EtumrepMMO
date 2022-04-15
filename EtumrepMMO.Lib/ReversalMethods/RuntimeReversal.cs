using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PKHeX.Core;

namespace EtumrepMMO.Lib;

/// <summary>
/// Picks out the entity-seed reversal algorithm
/// </summary>
public static class RuntimeReversal
{
    /// <summary>
    /// Enables using the C# Implementation if the faster native implementation fails to find a result.
    /// </summary>
    public static bool EnableFallbackReversal { get; set; }

    /// <summary>
    /// Enables using the native dll reversal library (if supported by the host operating system).
    /// </summary>
    public static bool UseNativeReversalLibrary { get; set; } = true;

    /// <summary>
    /// Finds all entity-seeds for the entity.
    /// </summary>
    /// <param name="pk">Entity to check</param>
    /// <param name="max_rolls">Maximum amount of shiny rolls permitted</param>
    /// <returns>Count of seed-rolls stored in the input spans.</returns>
    public static (ulong Seed, byte Rolls)[] GetSeeds(PKM pk, byte max_rolls)
    {
        if (pk.IsShiny)
        {
            Console.WriteLine("Shiny...");
            return GetSeedsRuntime(pk, max_rolls);
        }

        if (UseNativeReversalLibrary && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // The dll makes some assumptions in order to maximize performance for 99.9999% of cases.
            // If we fail to get a result (extremely unlikely), then fall back to the C# implementation.
            var dllResults = IterativeReversal.GetSeeds(pk, max_rolls);
            if (dllResults.Length != 0 || !EnableFallbackReversal)
                return dllResults;

            Console.WriteLine("Falling back to runtime search...");
        }

        // C# Implementation
        return GetSeedsRuntime(pk, max_rolls);
    }

    public static (ulong Seed, byte Rolls)[] GetSeedsRuntime(PKM pk, byte max_rolls)
    {
        Console.WriteLine("Searching with runtime search method...");
        var sw = new Stopwatch();
        sw.Start();
        var result = GetAllSeeds(pk, max_rolls);
        var msg = result.Count == 0 ? "No seeds found." : "Found seeds using runtime search method:";
        Console.WriteLine(msg);

        var map = new (ulong, byte)[result.Count];
        for (int i = 0; i < result.Count; i++)
        {
            var (seed, shinyRolls) = result[i];
            map[i] = (seed, (byte)shinyRolls);
            Console.WriteLine($"{seed:x} (rolls = {shinyRolls})");
        }

        Console.WriteLine($"Time: {sw.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine();
        return map;
    }

    private static List<SeedSearchResult> GetAllSeeds(PKM pk, byte max_rolls)
    {
        var result = new List<SeedSearchResult>();
        ConcurrentBag<ulong> seeds = new();

        if (pk.IsShiny)
        {
            FindPotentialSeedsShiny(seeds, pk.PID & 0xFFFF, pk.EncryptionConstant, max_rolls);
        }
        else
        {
            FindPotentialSeeds(seeds, pk.PID, pk.EncryptionConstant, max_rolls);
            if (IsPotentialAntiShiny(pk.TID, pk.SID, pk.PID))
                FindPotentialSeeds(seeds, pk.PID ^ 0x1000_0000, pk.EncryptionConstant, max_rolls);
        }

        foreach (var seed in seeds)
        {
            for (var cnt = 1; cnt <= max_rolls; cnt++)
            {
                for (int ivs = 0; ivs <= 4; ivs++)
                {
                    // Verify the IVs; only 0, 3, and 4 fixed IVs exist.
                    if (ivs is > 0 and < 3)
                        ivs = 3;

                    if (pk.FlawlessIVCount >= ivs && IsMatch(seed, ivs, cnt, pk))
                        result.Add(new SeedSearchResult(seed, cnt));
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Indicates if the input <see cref="pid"/> is an anti-shiny with the supplied trainer data.
    /// </summary>
    /// <param name="tid">16-bit trainer ID</param>
    /// <param name="sid">16-bit trainer ID (secret)</param>
    /// <param name="pid">32-bit entity personality value</param>
    /// <returns>True if was shiny, then made anti-shiny.</returns>
    public static bool IsPotentialAntiShiny(int tid, int sid, uint pid)
    {
        return GetIsShiny(tid, sid, pid ^ 0x1000_0000);
    }

    private static bool GetIsShiny(int tid, int sid, uint pid)
    {
        return GetShinyXor(pid, (uint)((sid << 16) | tid)) < 16;
    }

    private readonly record struct SeedSearchResult(ulong Seed, int ShinyRolls)
    {
        public readonly ulong Seed = Seed;
        public readonly int ShinyRolls = ShinyRolls;
    }

    private static void FindPotentialSeeds(ConcurrentBag<ulong> all_seeds, uint pid, uint ec, byte max_rolls)
    {
        ulong start_seed = ec - unchecked((uint)Xoroshiro128Plus.XOROSHIRO_CONST);
        Debug.WriteLine($"Iteration start seed: {start_seed:X16}");

        Parallel.For(0, 0xFFFF, i =>
        {
            var test = start_seed | (ulong)i << 48;
            for (int x = 0; x < 65536; x++)
            {
                var seed = CheckSeed(test, pid, max_rolls);
                if (seed != 0)
                    all_seeds.Add(seed);
                test += 0x1_0000_0000;
            }
        });
    }

    private static void FindPotentialSeedsShiny(ConcurrentBag<ulong> all_seeds, uint pid, uint ec, byte max_rolls)
    {
        ulong start_seed = ec - unchecked((uint)Xoroshiro128Plus.XOROSHIRO_CONST);
        Debug.WriteLine($"Iteration start seed: {start_seed:X16}");

        Parallel.For(0, 0xFFFF, i =>
        {
            var test = start_seed | (ulong)i << 48;
            for (int x = 0; x < 65536; x++)
            {
                var seed = CheckSeedShiny(test, pid, max_rolls);
                if (seed != 0)
                    all_seeds.Add(seed);
                test += 0x1_0000_0000;
            }
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CheckSeed(ulong seed, uint expectPID, byte max_rolls)
    {
        var rng = new Xoroshiro128Plus(seed);
        rng.NextInt(); // EC
        rng.NextInt(); // fakeTID

        for (int n = 1; n <= max_rolls; n++)
        {
            var pid = rng.NextInt();
            if (pid == expectPID)
                return seed;
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong CheckSeedShiny(ulong seed, uint expectPID, byte max_rolls)
    {
        var rng = new Xoroshiro128Plus(seed);
        rng.NextInt(); // EC
        rng.NextInt(); // fakeTID

        for (int n = 1; n <= max_rolls; n++)
        {
            var pid = rng.NextInt();
            if (expectPID == (pid & 0xFFFF))
                return seed;
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMatch(ulong seed, int fixed_ivs, int rolls, PKM pk)
    {
        int[] IVs = pk.IVs;
        PKX.ReorderSpeedLast(IVs);

        var rng = new Xoroshiro128Plus(seed);
        rng.NextInt(); // EC
        uint fakeTID = (uint)rng.NextInt(); // TID

        uint pid = 0;
        for (int i = 1; i <= rolls; i++)
            pid = (uint)rng.NextInt(); // PID

        // Record whether this was a shiny seed or not.
        var shiny_xor = GetShinyXor(pid, fakeTID);

        if (!pk.IsShiny && pk.PID != pid && pk.PID != (pid ^ 0x10000000))
            return false;
        if (pk.IsShiny && (pk.PID & 0xFFFF) != (pid & 0xFFFF))
            return false;
        if (pk.IsShiny && shiny_xor > 0xF)
            return false;

        Span<int> check_ivs = stackalloc int[6];
        check_ivs.Fill(-1);
        for (int i = 0; i < fixed_ivs; i++)
        {
            int slot;
            do
            {
                slot = (int)rng.NextInt(6);
            } while (check_ivs[slot] != -1);

            if (IVs[slot] != 31)
                return false;

            check_ivs[slot] = 31;
        }
        for (int i = 0; i < 6; i++)
        {
            if (check_ivs[i] != -1)
                continue; // already verified?

            int iv = (int)rng.NextInt(32);
            if (iv != IVs[i])
                return false;
        }

        var ability = (int)rng.NextInt(2) + 1; // Ability 1 or 2 only -- potentially could be changed with transfers?
        if (ability != pk.AbilityNumber)
            return false;

        var genderratio = PersonalTable.LA[pk.Species].Gender;
        if (genderratio is not (PersonalInfo.RatioMagicGenderless or PersonalInfo.RatioMagicFemale or PersonalInfo.RatioMagicMale))
        {
            var gender = (int)rng.NextInt(252) + 1 < genderratio ? 1 : 0; // Gender
            if (gender != pk.Gender)
                return false;
        }

        var nature = (int)rng.NextInt(25); // Nature -- no synchronize in LA
        if (nature != pk.Nature)
            return false;

        if (pk is IAlpha { IsAlpha: true})
            return true;

        if (pk is not IScaledSize s)
            return true;

        var height = (int)rng.NextInt(0x81) + (int)rng.NextInt(0x80);
        if (height != s.HeightScalar)
            return false;

        var weight = (int)rng.NextInt(0x81) + (int)rng.NextInt(0x80);
        if (weight != s.WeightScalar)
            return false;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetShinyXor(uint pid, uint oid)
    {
        var xor = pid ^ oid;
        return (xor ^ (xor >> 16)) & 0xFFFF;
    }

    // copy of xoroshiro so that the compiler can smartly optimize out some func calls

    /// <summary>
    /// Self-modifying RNG structure that implements xoroshiro128+
    /// </summary>
    /// <remarks>https://en.wikipedia.org/wiki/Xoroshiro128%2B</remarks>
    private ref struct Xoroshiro128Plus
    {
        private const ulong XOROSHIRO_CONST0 = 0x0F4B17A579F18960;
        public const ulong XOROSHIRO_CONST = 0x82A2B175229D6A5B;

        private ulong s0, s1;

        public Xoroshiro128Plus(ulong s0 = XOROSHIRO_CONST0, ulong s1 = XOROSHIRO_CONST) => (this.s0, this.s1) = (s0, s1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RotateLeft(ulong x, int k) => System.Numerics.BitOperations.RotateLeft(x, k);

        /// <summary>
        /// Gets the next random <see cref="ulong"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong Next()
        {
            var _s0 = s0;
            var _s1 = s1;
            ulong result = _s0 + _s1;

            _s1 ^= _s0;
            // Final calculations and store back to fields
            s0 = RotateLeft(_s0, 24) ^ _s1 ^ (_s1 << 16);
            s1 = RotateLeft(_s1, 37);

            return result;
        }

        /// <summary>
        /// Gets a random value that is less than <see cref="MOD"/>
        /// </summary>
        /// <param name="MOD">Maximum value (exclusive). Generates a bitmask for the loop.</param>
        /// <returns>Random value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong NextInt(ulong MOD = 0xFFFFFFFF)
        {
            ulong mask = GetBitmask(MOD);
            ulong res;
            do
            {
                res = Next() & mask;
            } while (res >= MOD);
            return res;
        }

        /// <summary>
        /// Next Power of Two
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GetBitmask(ulong x)
        {
            x--; // comment out to always take the next biggest power of two, even if x is already a power of two
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x;
        }
    }
}
