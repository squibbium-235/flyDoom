using FlyDoom.Connectome.Build;
using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;
using FlyDoom.Core.Biology;

namespace FlyDoom.Tests;

/// <summary>
/// Tests construction of compact per-neuron metadata.
/// </summary>
public sealed class CompactNeuronTableBuilderTests
{
    [Fact]
    public void Build_AlignsMetadataWithSimulationIndices()
    {
        var neuronIndexMap =
            NeuronIndexMap.Create(
            [
                100,
                200
            ]);

        // Deliberately reversed to prove that the builder uses the
        // neuron index map rather than relying on input order.
        var neurons =
            new List<FafbNeuronRecord>
            {
                new()
                {
                    RootId = 200,
                    NeurotransmitterType = "GABA",
                    NeurotransmitterTypeScore = 0.8,
                    GabaAverage = 0.75
                },
                new()
                {
                    RootId = 100,
                    NeurotransmitterType = "ACH",
                    NeurotransmitterTypeScore = 0.9,
                    AcetylcholineAverage = 0.85
                }
            };

        var table =
            CompactNeuronTableBuilder.Build(
                neurons,
                neuronIndexMap);

        Assert.Equal(2, table.Count);

        Assert.Equal(
            NeurotransmitterType.Acetylcholine,
            table.GetNeurotransmitterType(0));

        Assert.Equal(
            NeurotransmitterType.Gaba,
            table.GetNeurotransmitterType(1));

        Assert.Equal(
            0.9f,
            table.GetNeurotransmitterConfidence(0));

        Assert.Equal(
            0.8f,
            table.GetNeurotransmitterConfidence(1));

        Assert.Equal(
            0.85f,
            table.GetAcetylcholineScore(0));

        Assert.Equal(
            0.75f,
            table.GetGabaScore(1));
    }

    [Fact]
    public void Build_UsesNaNForMissingScores()
    {
        var neuronIndexMap =
            NeuronIndexMap.Create(
            [
                100
            ]);

        var neurons =
            new List<FafbNeuronRecord>
            {
                new()
                {
                    RootId = 100,
                    NeurotransmitterType = null
                }
            };

        var table =
            CompactNeuronTableBuilder.Build(
                neurons,
                neuronIndexMap);

        Assert.Equal(
            NeurotransmitterType.Unknown,
            table.GetNeurotransmitterType(0));

        Assert.True(
            float.IsNaN(
                table.GetNeurotransmitterConfidence(0)));

        Assert.True(
            float.IsNaN(
                table.GetDopamineScore(0)));
    }

    [Fact]
    public void Build_RejectsUnknownNeuron()
    {
        var neuronIndexMap =
            NeuronIndexMap.Create(
            [
                100
            ]);

        var neurons =
            new List<FafbNeuronRecord>
            {
                new()
                {
                    RootId = 999,
                    NeurotransmitterType = "ACH"
                }
            };

        Assert.Throws<InvalidDataException>(
            () => CompactNeuronTableBuilder.Build(
                neurons,
                neuronIndexMap));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void GetNeuronMetadata_RejectsInvalidIndex(
        int neuronIndex)
    {
        var neuronIndexMap =
            NeuronIndexMap.Create(
            [
                100,
                200
            ]);

        var neurons =
            new List<FafbNeuronRecord>
            {
                new()
                {
                    RootId = 100,
                    NeurotransmitterType = "ACH"
                },
                new()
                {
                    RootId = 200,
                    NeurotransmitterType = "GABA"
                }
            };

        var table =
            CompactNeuronTableBuilder.Build(
                neurons,
                neuronIndexMap);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => table.GetNeurotransmitterType(
                neuronIndex));
    }
}