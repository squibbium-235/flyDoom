using FlyDoom.Connectome.Model;
using FlyDoom.Core.Biology;
using FlyDoom.Neural.Transmission;

namespace FlyDoom.Tests;

public sealed class FastTransmitterSynapticEffectModelTests
{
    [Fact]
    public void Acetylcholine_ProducesPositiveInput()
    {
        var model =
            CreateModel();

        var input =
            model.CalculateInputAmplitudeMv(
                0,
                1,
                5,
                0,
                NeurotransmitterType.Acetylcholine);

        Assert.True(
            input > 0);
    }

    [Fact]
    public void Gaba_ProducesNegativeInput()
    {
        var model =
            CreateModel();

        var input =
            model.CalculateInputAmplitudeMv(
                0,
                1,
                5,
                0,
                NeurotransmitterType.Gaba);

        Assert.True(
            input < 0);
    }

    [Fact]
    public void Glutamate_ProducesNegativeInput()
    {
        var model =
            CreateModel();

        var input =
            model.CalculateInputAmplitudeMv(
                0,
                1,
                5,
                0,
                NeurotransmitterType.Glutamate);

        Assert.True(
            input < 0);
    }

    [Theory]
    [InlineData(NeurotransmitterType.Dopamine)]
    [InlineData(NeurotransmitterType.Serotonin)]
    [InlineData(NeurotransmitterType.Octopamine)]
    [InlineData(NeurotransmitterType.Unknown)]
    public void NonFastTransmitters_ProduceNoFastInput(
        NeurotransmitterType neurotransmitterType)
    {
        var model =
            CreateModel();

        var input =
            model.CalculateInputAmplitudeMv(
                0,
                1,
                5,
                0,
                neurotransmitterType);

        Assert.Equal(
            0f,
            input);
    }

    [Fact]
    public void Input_IsNormalisedByPostsynapticInput()
    {
        var connectome =
            CreateConnectome();

        var inputs =
            PostsynapticInputTable.Build(
                connectome);

        var model =
            new FastTransmitterSynapticEffectModel(
                inputs,
                fullInputAmplitudeMv: 100f);

        var input =
            model.CalculateInputAmplitudeMv(
                0,
                1,
                5,
                0,
                NeurotransmitterType.Acetylcholine);

        Assert.Equal(
            50f,
            input);
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
            fullInputAmplitudeMv: 40f);
    }

    private static CompactConnectome
        CreateConnectome()
    {
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