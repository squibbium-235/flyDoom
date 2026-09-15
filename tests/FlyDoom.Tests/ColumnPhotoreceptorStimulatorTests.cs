using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;
using FlyDoom.Core.Biology;
using FlyDoom.Neural.Simulation;
using FlyDoom.Neural.Transmission;
using FlyDoom.Vision.Build;
using FlyDoom.Vision.Stimulation;

namespace FlyDoom.Tests;

public sealed class ColumnPhotoreceptorStimulatorTests
{
    [Fact]
    public void Flash_AppliesInhibitoryInputFromR7AndR8()
    {
        var neuronMap =
            NeuronIndexMap.Create(
            [
                100,
                200,
                300
            ]);

        var catalog =
            VisualNeuronCatalogBuilder.Build(
                neuronMap,
                [
                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 100,
                        Type = "R7"
                    },

                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 200,
                        Type = "R8"
                    }
                ],
                [
                    new FafbColumnAssignmentRecord
                    {
                        RootId = 100,
                        Hemisphere = "right",
                        Type = "R7",
                        ColumnId = "1"
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 200,
                        Hemisphere = "right",
                        Type = "R8",
                        ColumnId = "1"
                    }
                ]);

        var columns =
            VisualColumnMapBuilder.Build(
                catalog);

        //
        // Both photoreceptors converge on neuron 2 with five anatomical
        // synapses each. Each therefore represents half of the target's
        // ten incoming synapses.
        //
        var connectome =
            new CompactConnectome(
                outgoingOffsets:
                [
                    0,
                    1,
                    2,
                    2
                ],
                postsynapticIndices:
                [
                    2,
                    2
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

        var inputs =
            PostsynapticInputTable.Build(
                connectome);

        var state =
            new NeuronStateTable(
                3,
                -60f);

        var stimulator =
            new ColumnPhotoreceptorStimulator(
                catalog,
                connectome,
                inputs,
                fullHistamineInputAmplitudeMv: 40f);

        var result =
            stimulator.ApplyColumnFlash(
                columns.GetColumn(0),
                state,
                intensity: 1f);

        Assert.Equal(
            2,
            result.PhotoreceptorCount);

        Assert.Equal(
            2,
            result.AppliedConnectionCount);

        Assert.Equal(
            1,
            result.UniqueTargetCount);

        Assert.Equal(
            -40f,
            state.GetSynapticInputMv(2));

        Assert.Equal(
            -40f,
            result.MostNegativeInputAmplitudeMv);
    }

    [Fact]
    public void Flash_IgnoresNonPhotoreceptorColumnNeurons()
    {
        var neuronMap =
            NeuronIndexMap.Create(
            [
                100,
                200
            ]);

        var catalog =
            VisualNeuronCatalogBuilder.Build(
                neuronMap,
                [
                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 100,
                        Type = "Mi1"
                    }
                ],
                [
                    new FafbColumnAssignmentRecord
                    {
                        RootId = 100,
                        Hemisphere = "right",
                        Type = "Mi1",
                        ColumnId = "1"
                    }
                ]);

        var columns =
            VisualColumnMapBuilder.Build(
                catalog);

        var connectome =
            new CompactConnectome(
                outgoingOffsets:
                [
                    0,
                    1,
                    1
                ],
                postsynapticIndices:
                [
                    1
                ],
                synapseCounts:
                [
                    5
                ],
                neuropilIndices:
                [
                    0
                ],
                neurotransmitterTypes:
                [
                    NeurotransmitterType.Acetylcholine
                ],
                neuropilNames:
                [
                    "TEST"
                ]);

        var inputs =
            PostsynapticInputTable.Build(
                connectome);

        var state =
            new NeuronStateTable(
                2,
                -60f);

        var stimulator =
            new ColumnPhotoreceptorStimulator(
                catalog,
                connectome,
                inputs,
                40f);

        var result =
            stimulator.ApplyColumnFlash(
                columns.GetColumn(0),
                state,
                1f);

        Assert.Equal(
            0,
            result.PhotoreceptorCount);

        Assert.Equal(
            0,
            result.AppliedConnectionCount);

        Assert.Equal(
            0f,
            state.GetSynapticInputMv(1));
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Flash_RejectsInvalidIntensity(
    float intensity)
    {
        var neuronMap =
            NeuronIndexMap.Create(
            [
                100
            ]);

        var catalog =
            VisualNeuronCatalogBuilder.Build(
                neuronMap,
                [
                    new FafbVisualNeuronTypeRecord
                {
                    RootId = 100,
                    Type = "R7"
                }
                ],
                [
                    new FafbColumnAssignmentRecord
                {
                    RootId = 100,
                    Hemisphere = "right",
                    Type = "R7",
                    ColumnId = "1"
                }
                ]);

        var columns =
            VisualColumnMapBuilder.Build(
                catalog);

        var connectome =
            new CompactConnectome(
                outgoingOffsets:
                [
                    0,
                0
                ],
                postsynapticIndices:
                [],
                synapseCounts:
                [],
                neuropilIndices:
                [],
                neurotransmitterTypes:
                [],
                neuropilNames:
                []);

        var inputs =
            PostsynapticInputTable.Build(
                connectome);

        var state =
            new NeuronStateTable(
                1,
                -60f);

        var stimulator =
            new ColumnPhotoreceptorStimulator(
                catalog,
                connectome,
                inputs,
                40f);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                stimulator.ApplyColumnFlash(
                    columns.GetColumn(0),
                    state,
                    intensity));
    }
}