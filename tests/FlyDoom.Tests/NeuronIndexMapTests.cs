using FlyDoom.Connectome.Model;

namespace FlyDoom.Tests;

/// <summary>
/// Tests the mapping between FlyWire root IDs and compact simulation indices.
/// </summary>
public sealed class NeuronIndexMapTests
{
    /// <summary>
    /// Verifies that root IDs are assigned contiguous indices in input order.
    /// </summary>
    [Fact]
    public void Create_AssignsContiguousIndices()
    {
        var rootIds = new long[]
        {
            1001,
            2002,
            3003
        };

        var map = NeuronIndexMap.Create(rootIds);

        Assert.Equal(3, map.Count);

        Assert.Equal(0, map.GetIndex(1001));
        Assert.Equal(1, map.GetIndex(2002));
        Assert.Equal(2, map.GetIndex(3003));
    }

    /// <summary>
    /// Verifies that simulation indices can be mapped back to their original
    /// FlyWire root IDs.
    /// </summary>
    [Fact]
    public void GetRootId_ReturnsOriginalRootId()
    {
        var rootIds = new long[]
        {
            1001,
            2002,
            3003
        };

        var map = NeuronIndexMap.Create(rootIds);

        Assert.Equal(1001, map.GetRootId(0));
        Assert.Equal(2002, map.GetRootId(1));
        Assert.Equal(3003, map.GetRootId(2));
    }

    /// <summary>
    /// Verifies that an unknown FlyWire root ID is reported as missing.
    /// </summary>
    [Fact]
    public void TryGetIndex_ReturnsFalseForUnknownRootId()
    {
        var map = NeuronIndexMap.Create(
            new long[]
            {
                1001,
                2002
            });

        var found = map.TryGetIndex(
            9999,
            out var index);

        Assert.False(found);
        Assert.Equal(0, index);
    }

    /// <summary>
    /// Verifies that duplicate FlyWire root IDs are rejected.
    /// </summary>
    [Fact]
    public void Create_ThrowsForDuplicateRootIds()
    {
        var rootIds = new long[]
        {
            1001,
            2002,
            1001
        };

        Assert.Throws<InvalidDataException>(
            () => NeuronIndexMap.Create(rootIds));
    }

    /// <summary>
    /// Verifies that invalid simulation indices are rejected.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void GetRootId_ThrowsForInvalidIndex(int index)
    {
        var map = NeuronIndexMap.Create(
            new long[]
            {
                1001,
                2002,
                3003
            });

        Assert.Throws<ArgumentOutOfRangeException>(
            () => map.GetRootId(index));
    }
}