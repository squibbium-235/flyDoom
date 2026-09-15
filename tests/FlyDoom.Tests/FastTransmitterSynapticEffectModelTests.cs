using FlyDoom.Connectome.Model;
using FlyDoom.Core.Biology;
using FlyDoom.Neural.Transmission;

namespace FlyDoom.Tests;

public sealed class FastTransmitterSynapticEffectModelTests
{
    [Fact]
    public void Acetylcholine_ProducesPositiveDrive()
    {
        var model =
            CreateModel();

        var drive =
            model.CalculateDriveMv(
                0,
                1,
                5,
                0,
                NeurotransmitterType.Acetylcholine);

        Assert.True(
            drive > 0);
    }

    [Fact]
    public void Gaba_ProducesNegativeDrive()
    {
        var model =
            CreateModel();

        var drive =
            model.CalculateDriveMv(
                0,
                1,
                5,
                0,
                NeurotransmitterType.Gaba);

        Assert.True(
            drive < 0);
    }

    [Fact]
    public void Glutamate_ProducesNegativeDrive()
    {
        var model =
            CreateModel();

        var drive =
            model.CalculateDriveMv(
                0,
                1,
                5,
                0,
                NeurotransmitterType.Glutamate);

        Assert.True(
            drive < 0);
    }

    [Theory]
    [InlineData(NeurotransmitterType.Dopamine)]
    [InlineData(NeurotransmitterType.Serotonin)]
    [InlineData(NeurotransmitterType.Octopamine)]
    [InlineData(NeurotransmitterType.Unknown)]
    public void NonFastTransmitters_DoNotProduceFastDrive(
        NeurotransmitterType neurotransmitterType)
    {
        var model =
            CreateModel();

        var drive =
            model.CalculateDriveMv(
                0,
                1,
                5,
                0,
                neurotransmitterType);

        Assert.Equal(
            0f,
            drive);
    }

    [Fact]
    public void Drive_IsNormalisedByPostsynapticInput()
    {
        var connectome =
            CreateConnectome();

        var inputs =
            PostsynapticInputTable.Build(
                connectome);

        var model =
            new FastTransmitterSynapticEffectModel(
                inputs,
                fullInputDriveMv: 100f);

        // Neuron 1 receives 5 + 5 = 10 anatomical synapses.
        // One five-synapse connection therefore represents 50% of
        // its total anatomical input.
        var drive =
            model.CalculateDriveMv(
                0,
                1,
                5,
                0,
                NeurotransmitterType.Acetylcholine);

        Assert.Equal(
            50f,
            drive);
    }

    [Fact]
    public void InputTable_SumsIncomingSynapses()
    {
        var connectome =
            CreateConnectome();

        var inputs =
            PostsynapticInputTable.Build(
                connectome);

        Assert.Equal(
            10,
            inputs.GetTotalIncomingSynapses(1));
    }

    private static FastTransmitterSynapticEffectModel
        CreateModel()
    {
        var connectome =
            CreateConnectome();

        var inputs =
            PostsynapticInputTable.Build(
                connectome);

        return new FastTransmitterSynapticEffectModel(
            inputs,
            fullInputDriveMv: 40f);
    }

    private static CompactConnectome
        CreateConnectome()
    {
        // Neuron 0 -> neuron 1 with five synapses.
        // Neuron 2 -> neuron 1 with five synapses.
        return new CompactConnectome(
            outgoingOffsets:
            [
                0,
                1,
                1,
                2
            ],
            postsynapticIndices:
            [
                1,
                1
            ],
            synapseCounts:
            [
                5,
                5
            ],
            neuropilIndices:
            [
                0,
                0
            ],
            neurotransmitterTypes:
            [
                NeurotransmitterType.Acetylcholine,
                NeurotransmitterType.Acetylcholine
            ],
            neuropilNames:
            [
                "TEST"
            ]);
    }
}