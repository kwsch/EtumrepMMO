using EtumrepMMO.Lib;
using FluentAssertions;
using PKHeX.Core;
using Xunit;

namespace EtumrepMMO.Tests;

public class ReversalTests
{
    [Fact]
    public void TestReversal()
    {
        var pk1 = Properties.Resources.Tentacool1;
        var pk2 = Properties.Resources.Tentacool2;
        var pk3 = Properties.Resources.Tentacool3;
        var pk4 = Properties.Resources.Tentacool4;
        const int rollCount = 17;

        var all = new[] { pk1, pk2, pk3, pk4 }.Select(z => new PA8(z)).ToArray();
        foreach (var d in all)
        {
            var seeds = IterativeReversal.GetSeeds(d, rollCount);
            seeds.Should().NotBeEmpty();
        }

        var groupSeed = GroupSeedFinder.FindSeed(all, rollCount, SpawnerType.MultiSpawn);
        groupSeed.Should().NotBe(default);
    }

    [Fact]
    public void TestRuntime()
    {
        var pk = Properties.Resources.Tentacool1;
        const int rollCount = 17;

        var seeds = RuntimeReversal.GetSeeds(new PA8(pk), rollCount);
        seeds.Should().NotBeEmpty();
    }

    [Fact]
    public void TestSingle()
    {
        var pk1 = new PA8(Properties.Resources.Snorlax1);
        var pk2 = new PA8(Properties.Resources.Snorlax2);
        const int rollCount = 5;

        var pk = new[] { pk1, pk2 };
        var (seed, firstIndex) = GroupSeedFinder.FindSeed(pk, rollCount, SpawnerType.SingleSpawn);
        seed.Should().Be(0x38dd607647e5b2b5);
        firstIndex.Should().Be(1);
    }

    [Theory]
    [InlineData(0xce662cc305201801, 0x5108de3827bd825c)]
    public void ReverseStep1(ulong seedGroup, ulong seedGen)
    {
        var xoro = new Xoroshiro128Plus(seedGroup);
        var expectGen = xoro.Next();
        expectGen.Should().Be(seedGen);

        var s0 = unchecked(seedGen - Xoroshiro128Plus.XOROSHIRO_CONST);
        s0.Should().Be(seedGroup);

        GroupSeedReversal.GetGroupSeed(seedGen).Should().Be(seedGroup);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(0xfcca2321c7d655ed, 0xad819080a1effcf6u)]
    [InlineData(0x366a1a7ed65e146c, 0x041b4ef9172f53f3u, 0xd9d1e54df50036ecu)]
    [InlineData(0xa69d3c25666a8c6a, 0x323ff4f71fb9898cu, 0x3d8d7e995f7569feu, 0x0eec4cffd2595d1bu)]
    public void ReverseStep2(ulong seedPoke, params ulong[] seedGenPossible)
    {
        foreach (var seedGen in seedGenPossible)
        {
            var xoro = new Xoroshiro128Plus(seedGen);
            _ = xoro.Next(); // slot
            var expectPoke = xoro.Next();
            expectPoke.Should().Be(seedPoke);
        }

        var reversal = GenSeedReversal.FindPotentialGenSeeds(seedPoke).ToArray();
        if (seedGenPossible.Length == 0)
        {
            reversal.Length.Should().Be(0);
            return;
        }

        // check for sequence equality, any order
        seedGenPossible.Should().Contain(reversal);
        reversal.Should().Contain(seedGenPossible);
    }

    [Theory]
    [InlineData(15156, 10217, 0xD9ECD53B)]
    public void IsPotentialShiny(ushort tid, ushort sid, uint pid)
    {
        var result = RuntimeReversal.IsPotentialAntiShiny(tid, sid, pid);
        result.Should().BeTrue();
    }
}
