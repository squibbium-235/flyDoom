using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;
using FlyDoom.Core.Biology;
using FlyDoom.Vision.Build;

namespace FlyDoom.Tests;

public sealed class R1R6CartridgeMapBuilderTests
{
    [Fact]
    public void Build_MapsPhotoreceptorWhenL1AndL2Agree()
    {
        var neuronMap =
            NeuronIndexMap.Create(
            [
                100,
                200,
                300,
                400,
                500
            ]);

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
                        Type = "L1"
                    },

                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 300,
                        Type = "L2"
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
                        Type = "L1",
                        ColumnId = "42"
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 300,
                        Hemisphere = "right",
                        Type = "L2",
                        ColumnId = "42"
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 400,
                        Hemisphere = "right",
                        Type = "L1",
                        ColumnId = "43"
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 500,
                        Hemisphere = "right",
                        Type = "L2",
                        ColumnId = "43"
                    }
                ]);

        var connectome =
            new CompactConnectome(
                outgoingOffsets:
                [
                    0,
                    4,
                    4,
                    4,
                    4,
                    4
                ],
                postsynapticIndices:
                [
                    1,
                    2,
                    3,
                    4
                ],
                synapseCounts:
                [
                    12,
                    10,
                    2,
                    1
                ],
                neuropilIndices:
                [
                    0,
                    0,
                    0,
                    0
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
                    "LA_R"
                ]);

        var map =
            R1R6CartridgeMapBuilder.Build(
                catalog,
                connectome);

        Assert.Equal(
            1,
            map.CandidatePhotoreceptorCount);

        Assert.Equal(
            1,
            map.MappedPhotoreceptorCount);

        Assert.Equal(
            0,
            map.UnmappedPhotoreceptorCount);

        Assert.True(
            map.TryGetAssignment(
                0,
                out var assignment));

        Assert.Equal(
            "right",
            assignment.Hemisphere);

        Assert.Equal(
            "42",
            assignment.ColumnId);

        Assert.Equal(
            12,
            assignment.L1SynapseCount);

        Assert.Equal(
            10,
            assignment.L2SynapseCount);

        Assert.Equal(
            25,
            assignment.TotalLandmarkSynapseCount);

        Assert.Equal(
            22.0 / 25.0,
            assignment.SupportFraction,
            precision: 6);
    }

    [Fact]
    public void Build_LeavesPhotoreceptorUnmappedWhenL1AndL2Disagree()
    {
        var neuronMap =
            NeuronIndexMap.Create(
            [
                100,
                200,
                300,
                400,
                500
            ]);

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
                        Type = "L1"
                    },

                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 300,
                        Type = "L2"
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
                        Type = "L1",
                        ColumnId = "42"
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 300,
                        Hemisphere = "right",
                        Type = "L2",
                        ColumnId = "42"
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 400,
                        Hemisphere = "right",
                        Type = "L1",
                        ColumnId = "43"
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 500,
                        Hemisphere = "right",
                        Type = "L2",
                        ColumnId = "43"
                    }
                ]);

        //
        // L1 says column 42, but L2 says column 43.
        //

        var connectome =
            new CompactConnectome(
                outgoingOffsets:
                [
                    0,
                    4,
                    4,
                    4,
                    4,
                    4
                ],
                postsynapticIndices:
                [
                    1,
                    2,
                    3,
                    4
                ],
                synapseCounts:
                [
                    10,
                    1,
                    2,
                    12
                ],
                neuropilIndices:
                [
                    0,
                    0,
                    0,
                    0
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
                    "LA_R"
                ]);

        var map =
            R1R6CartridgeMapBuilder.Build(
                catalog,
                connectome);

        Assert.Equal(
            1,
            map.CandidatePhotoreceptorCount);

        Assert.Equal(
            0,
            map.MappedPhotoreceptorCount);

        Assert.Equal(
            1,
            map.UnmappedPhotoreceptorCount);

        Assert.False(
            map.TryGetAssignment(
                0,
                out _));
    }

    [Fact]
    public void Build_IgnoresConnectionsOutsideLamina()
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
                        Type = "R1-6"
                    },

                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 200,
                        Type = "L1"
                    },

                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 300,
                        Type = "L2"
                    }
                ],
                [
                    new FafbColumnAssignmentRecord
                    {
                        RootId = 200,
                        Hemisphere = "right",
                        Type = "L1",
                        ColumnId = "42"
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 300,
                        Hemisphere = "right",
                        Type = "L2",
                        ColumnId = "42"
                    }
                ]);

        var connectome =
            new CompactConnectome(
                outgoingOffsets:
                [
                    0,
                    2,
                    2,
                    2
                ],
                postsynapticIndices:
                [
                    1,
                    2
                ],
                synapseCounts:
                [
                    10,
                    10
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
                    "ME_R"
                ]);

        var map =
            R1R6CartridgeMapBuilder.Build(
                catalog,
                connectome);

        Assert.Equal(
            0,
            map.MappedPhotoreceptorCount);
    }
}