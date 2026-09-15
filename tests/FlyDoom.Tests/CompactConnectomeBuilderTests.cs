using FlyDoom.Connectome.Build;
using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;

namespace FlyDoom.Tests;

/// <summary>
/// Tests construction of compact connectivity from FAFB records.
/// </summary>
public sealed class CompactConnectomeBuilderTests
{
    /// <summary>
    /// Verifies that connections are grouped by their presynaptic neuron
    /// while retaining their target, synapse count and neuropil.
    /// </summary>
    [Fact]
    public void Build_GroupsConnectionsByPresynapticNeuron()
    {
        var neuronIndexMap =
            NeuronIndexMap.Create(
            [
                100,
                200,
                300,
                400
            ]);

        var connections =
            new List<FafbConnectionRecord>
            {
                new()
                {
                    PresynapticRootId = 200,
                    PostsynapticRootId = 400,
                    SynapseCount = 3,
                    Neuropil = "ME_R"
                },
                new()
                {
                    PresynapticRootId = 100,
                    PostsynapticRootId = 300,
                    SynapseCount = 5,
                    Neuropil = "LO_R"
                },
                new()
                {
                    PresynapticRootId = 100,
                    PostsynapticRootId = 200,
                    SynapseCount = 2,
                    Neuropil = "ME_R"
                }
            };

        var connectome =
            CompactConnectomeBuilder.Build(
                neuronIndexMap,
                () => connections);

        Assert.Equal(4, connectome.NeuronCount);
        Assert.Equal(3, connectome.ConnectionCount);
        Assert.Equal(2, connectome.NeuropilCount);

        Assert.Equal(
            new[] { 2, 1 },
            connectome
                .GetPostsynapticIndices(0)
                .ToArray());

        Assert.Equal(
            new[] { 5, 2 },
            connectome
                .GetSynapseCounts(0)
                .ToArray());

        Assert.Equal(
            new[] { 3 },
            connectome
                .GetPostsynapticIndices(1)
                .ToArray());

        var neuropils =
            connectome
                .GetNeuropilIndices(0)
                .ToArray();

        Assert.Equal(2, neuropils.Length);

        Assert.Equal(
            "LO_R",
            connectome.GetNeuropilName(neuropils[0]));

        Assert.Equal(
            "ME_R",
            connectome.GetNeuropilName(neuropils[1]));
    }

    /// <summary>
    /// Verifies that connections referencing an unknown neuron are rejected.
    /// </summary>
    [Fact]
    public void Build_RejectsUnknownNeuron()
    {
        var neuronIndexMap =
            NeuronIndexMap.Create(
            [
                100,
                200
            ]);

        var connections =
            new List<FafbConnectionRecord>
            {
                new()
                {
                    PresynapticRootId = 100,
                    PostsynapticRootId = 999,
                    SynapseCount = 1,
                    Neuropil = "ME_R"
                }
            };

        Assert.Throws<InvalidDataException>(
            () => CompactConnectomeBuilder.Build(
                neuronIndexMap,
                () => connections));
    }

    /// <summary>
    /// Verifies that connections with non-positive synapse counts are rejected.
    /// </summary>
    [Fact]
    public void Build_RejectsInvalidSynapseCount()
    {
        var neuronIndexMap =
            NeuronIndexMap.Create(
            [
                100,
                200
            ]);

        var connections =
            new List<FafbConnectionRecord>
            {
                new()
                {
                    PresynapticRootId = 100,
                    PostsynapticRootId = 200,
                    SynapseCount = 0,
                    Neuropil = "ME_R"
                }
            };

        Assert.Throws<InvalidDataException>(
            () => CompactConnectomeBuilder.Build(
                neuronIndexMap,
                () => connections));
    }
}