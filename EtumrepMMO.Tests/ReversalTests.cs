﻿using System.Linq;
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

        var groupSeed = GroupSeedFinder.FindSeed(all, rollCount);
        groupSeed.Should().NotBe(default);
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
    [InlineData(0xad819080a1effcf6, 0xfcca2321c7d655ed)]
    public void ReverseStep2(ulong seedGen, ulong seedPoke)
    {
        var xoro = new Xoroshiro128Plus(seedGen);
        _ = xoro.Next(); // slot
        var expectPoke = xoro.Next();
        expectPoke.Should().Be(seedPoke);

        var reversal = GenSeedReversal.FindPotentialGenSeeds(seedPoke);
        reversal.Single().Should().Be(seedGen);
    }
}
