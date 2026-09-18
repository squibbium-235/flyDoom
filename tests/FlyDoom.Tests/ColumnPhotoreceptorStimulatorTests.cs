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
    public void Flash_UsesR1R6R7AndR8Photoreceptors()
    {
        var neuronMap =
            NeuronIndexMap.Create(
            [
                100,
                200,
                300,
                400,
                500,
                600
            ]);

        //
        // 0 = R1-R6
        // 1 = R7
        // 2 = R8
        // 3 = L1
        // 4 = L2
        // 5 = another postsynaptic target
        //

        var catalog =
            VisualNeuronCatalogBuilder.Build(
                neuronMap,
                [
                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 100,
                        Type = "R1-6"
                    },

                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 200,
                        Type = "R7"
                    },

                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 300,
                        Type = "R8"
                    },

                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 400,
                        Type = "L1"
                    },

                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 500,
                        Type = "L2"
                    }
                ],
                [
                    new FafbColumnAssignmentRecord
                    {
                        RootId = 200,
                        Hemisphere = "right",
                        Type = "R7",
                        ColumnId = "42"
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 300,
                        Hemisphere = "right",
                        Type = "R8",
                        ColumnId = "42"
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 400,
                        Hemisphere = "right",
                        Type = "L1",
                        ColumnId = "42"
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 500,
                        Hemisphere = "right",
                        Type = "L2",
                        ColumnId = "42"
                    }
                ]);

        var columns =
            VisualColumnMapBuilder.Build(
                catalog);

        var column =
            columns.Columns.Single(
                column =>
                    column.Hemisphere == "right" &&
                    column.ColumnId == "42");

        var connectome =
            new CompactConnectome(
                outgoingOffsets:
                [
                    0,
                    2,
                    3,
                    4,
                    4,
                    4,
                    4
                ],
                postsynapticIndices:
                [
                    3,
                    4,
                    5,
                    5
                ],
                synapseCounts:
                [
                    5,
                    5,
                    4,
                    6
                ],
                neuropilIndices:
                [
                    0,
                    0,
                    1,
                    1
                ],
                neurotransmitterTypes:
                [
                    NeurotransmitterType.Acetylcholine,
                    NeurotransmitterType.Acetylcholine,
                    NeurotransmitterType.Acetylcholine,
                    NeurotransmitterType.Acetylcholine
                ],
                neuropilNames:
                [
                    "LA_R",
                    "ME_R"
                ]);

        var r1R6Map =
            R1R6CartridgeMapBuilder.Build(
                catalog,
                connectome);

        Assert.Equal(
            1,
            r1R6Map.MappedPhotoreceptorCount);

        var inputs =
            PostsynapticInputTable.Build(
                connectome);

        var state =
            new NeuronStateTable(
                6,
                -60f);

        var stimulator =
            new ColumnPhotoreceptorStimulator(
                catalog,
                connectome,
                inputs,
                r1R6Map,
                fullHistamineInputAmplitudeMv: 40f);

        var result =
            stimulator.ApplyColumnFlash(
                column,
                state,
                intensity: 1f);

        Assert.Equal(
            3,
            result.PhotoreceptorCount);

        Assert.Equal(
            4,
            result.AppliedConnectionCount);

        Assert.Equal(
            3,
            result.UniqueTargetCount);

        Assert.Equal(
            -40f,
            state.GetSynapticInputMv(3));

        Assert.Equal(
            -40f,
            state.GetSynapticInputMv(4));

        Assert.Equal(
            -40f,
            state.GetSynapticInputMv(5));

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
                    "ME_R"
                ]);

        var inputs =
            PostsynapticInputTable.Build(
                connectome);

        var state =
            new NeuronStateTable(
                2,
                -60f);

        //
        // Use the backwards-compatible constructor with no R1-R6 map.
        //

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