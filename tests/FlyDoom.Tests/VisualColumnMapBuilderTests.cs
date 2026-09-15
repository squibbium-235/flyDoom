using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;
using FlyDoom.Vision.Build;

namespace FlyDoom.Tests;

public sealed class VisualColumnMapBuilderTests
{
    [Fact]
    public void Build_GroupsDifferentTypesIntoSameSpatialColumn()
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
                        Type = "L1"
                    },

                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 200,
                        Type = "Mi1"
                    }
                ],
                [
                    new FafbColumnAssignmentRecord
                    {
                        RootId = 100,
                        Hemisphere = "left",
                        Type = "L1",
                        ColumnId = "42",
                        X = 10,
                        Y = 20,
                        P = 30,
                        Q = 40
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 200,
                        Hemisphere = "left",
                        Type = "Mi1",
                        ColumnId = "42",
                        X = 14,
                        Y = 24,
                        P = 34,
                        Q = 44
                    }
                ]);

        var columns =
            VisualColumnMapBuilder.Build(
                catalog);

        Assert.Single(
            columns.Columns);

        var column =
            columns.GetColumn(0);

        Assert.Equal(
            2,
            column.NeuronCount);

        Assert.Equal(
            "left",
            column.Hemisphere);

        Assert.Equal(
            "42",
            column.ColumnId);

        Assert.Equal(
            12d,
            column.X);

        Assert.Equal(
            22d,
            column.Y);
    }

    [Fact]
    public void Build_SeparatesSameColumnIdAcrossHemispheres()
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
                [],
                [
                    new FafbColumnAssignmentRecord
                    {
                        RootId = 100,
                        Hemisphere = "left",
                        Type = "L1",
                        ColumnId = "10"
                    },

                    new FafbColumnAssignmentRecord
                    {
                        RootId = 200,
                        Hemisphere = "right",
                        Type = "L1",
                        ColumnId = "10"
                    }
                ]);

        var columns =
            VisualColumnMapBuilder.Build(
                catalog);

        Assert.Equal(
            2,
            columns.Count);
    }

    [Fact]
    public void Build_IgnoresNeuronsWithoutColumnAssignment()
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
                        Type = "R1-6"
                    }
                ],
                []);

        var columns =
            VisualColumnMapBuilder.Build(
                catalog);

        Assert.Empty(
            columns.Columns);
    }
}