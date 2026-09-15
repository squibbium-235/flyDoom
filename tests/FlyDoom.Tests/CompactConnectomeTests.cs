using FlyDoom.Connectome.Model;
using FlyDoom.Core.Biology;

namespace FlyDoom.Tests;

/// <summary>
/// Tests the compact representation of connectome connectivity.
/// </summary>
public sealed class CompactConnectomeTests
{
    [Fact]
    public void Constructor_SetsNeuronAndConnectionCounts()
    {
        var connectome = CreateTestConnectome();

        Assert.Equal(3, connectome.NeuronCount);
        Assert.Equal(6, connectome.ConnectionCount);
    }

    [Fact]
    public void GetPostsynapticIndices_ReturnsOutgoingTargets()
    {
        var connectome = CreateTestConnectome();

        var targets = connectome
            .GetPostsynapticIndices(0)
            .ToArray();

        Assert.Equal(
            new[]
            {
                4,
                7,
                9
            },
            targets);
    }

    [Fact]
    public void GetSynapseCounts_ReturnsMatchingSynapseCounts()
    {
        var connectome = CreateTestConnectome();

        var synapseCounts = connectome
            .GetSynapseCounts(0)
            .ToArray();

        Assert.Equal(
            new[]
            {
                2,
                5,
                1
            },
            synapseCounts);
    }

    [Fact]
    public void NeuronWithOneConnection_ReturnsOneEntry()
    {
        var connectome = CreateTestConnectome();

        Assert.Equal(
            new[] { 3 },
            connectome.GetPostsynapticIndices(1).ToArray());

        Assert.Equal(
            new[] { 4 },
            connectome.GetSynapseCounts(1).ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void GetOutgoingConnections_ThrowsForInvalidNeuronIndex(
        int neuronIndex)
    {
        var connectome = CreateTestConnectome();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => connectome
                .GetPostsynapticIndices(neuronIndex));
    }

    private static CompactConnectome CreateTestConnectome()
    {
        return new CompactConnectome(
            outgoingOffsets:
            [
                0,
                3,
                4,
                6
            ],
            postsynapticIndices:
            [
                4,
                7,
                9,
                3,
                1,
                8
            ],
            synapseCounts:
            [
                2,
                5,
                1,
                4,
                7,
                3
            ],
            neuropilIndices:
            [
                0,
                1,
                0,
                2,
                1,
                2
            ],
            neurotransmitterTypes:
            [
                NeurotransmitterType.Acetylcholine,
                NeurotransmitterType.Gaba,
                NeurotransmitterType.Acetylcholine,
                NeurotransmitterType.Glutamate,
                NeurotransmitterType.Dopamine,
                NeurotransmitterType.Serotonin
            ],
            neuropilNames:
            [
                "ME_L",
                "LO_L",
                "LOP_L"
            ]);
    }

    [Fact]
    public void GetNeuropilIndices_ReturnsMatchingNeuropils()
    {
        var connectome = CreateTestConnectome();

        Assert.Equal(
            new ushort[] { 0, 1, 0 },
            connectome
                .GetNeuropilIndices(0)
                .ToArray());
    }

    [Fact]
    public void GetNeuropilName_ReturnsOriginalName()
    {
        var connectome = CreateTestConnectome();

        Assert.Equal(
            "ME_L",
            connectome.GetNeuropilName(0));

        Assert.Equal(
            "LO_L",
            connectome.GetNeuropilName(1));

        Assert.Equal(
            "LOP_L",
            connectome.GetNeuropilName(2));
    }
}