using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;
using FlyDoom.Vision.Build;

namespace FlyDoom.Tests;

public sealed class VisualNeuronCatalogBuilderTests
{
    [Fact]
    public void Build_AlignsVisualMetadataWithNeuronIndices()
    {
        var map =
            NeuronIndexMap.Create(
            [
                100,
                200
            ]);

        var visualRecords =
            new[]
            {
                new FafbVisualNeuronTypeRecord
                {
                    RootId = 200,
                    Type = "Type B",
                    Family = "Family B",
                    Subsystem = "Subsystem B",
                    Category = "Category B",
                    Side = "right"
                },

                new FafbVisualNeuronTypeRecord
                {
                    RootId = 100,
                    Type = "Type A",
                    Family = "Family A",
                    Subsystem = "Subsystem A",
                    Category = "Category A",
                    Side = "left"
                }
            };

        var columnRecords =
            new[]
            {
                new FafbColumnAssignmentRecord
                {
                    RootId = 100,
                    Hemisphere = "left",
                    Type = "Type A",
                    ColumnId = "42",
                    X = 10,
                    Y = 20,
                    P = 30,
                    Q = 40
                }
            };

        var catalog =
            VisualNeuronCatalogBuilder.Build(
                map,
                visualRecords,
                columnRecords);

        Assert.Equal(
            "Type A",
            catalog.GetType(0));

        Assert.Equal(
            "Type B",
            catalog.GetType(1));

        Assert.True(
            catalog.IsVisualNeuron(0));

        Assert.True(
            catalog.HasColumnAssignment(0));

        Assert.False(
            catalog.HasColumnAssignment(1));

        Assert.Equal(
            "42",
            catalog.GetColumnId(0));

        Assert.Equal(
            10,
            catalog.GetColumnX(0));
    }

    [Fact]
    public void Build_UsesNullAndNaNForMissingColumnData()
    {
        var map =
            NeuronIndexMap.Create(
            [
                100
            ]);

        var catalog =
            VisualNeuronCatalogBuilder.Build(
                map,
                [
                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 100,
                        Type = "Test"
                    }
                ],
                []);

        Assert.False(
            catalog.HasColumnAssignment(0));

        Assert.Null(
            catalog.GetColumnId(0));

        Assert.True(
            double.IsNaN(
                catalog.GetColumnX(0)));
    }

    [Fact]
    public void Build_NormalisesBlankStrings()
    {
        var map =
            NeuronIndexMap.Create(
            [
                100
            ]);

        var catalog =
            VisualNeuronCatalogBuilder.Build(
                map,
                [
                    new FafbVisualNeuronTypeRecord
                    {
                        RootId = 100,
                        Type = "   "
                    }
                ],
                []);

        Assert.Null(
            catalog.GetType(0));
    }

    [Fact]
    public void Build_RejectsUnknownNeuron()
    {
        var map =
            NeuronIndexMap.Create(
            [
                100
            ]);

        Assert.Throws<InvalidDataException>(
            () =>
                VisualNeuronCatalogBuilder.Build(
                    map,
                    [
                        new FafbVisualNeuronTypeRecord
                        {
                            RootId = 999,
                            Type = "Test"
                        }
                    ],
                    []));
    }

    [Fact]
    public void Build_RejectsDuplicateVisualAnnotation()
    {
        var map =
            NeuronIndexMap.Create(
            [
                100
            ]);

        Assert.Throws<InvalidDataException>(
            () =>
                VisualNeuronCatalogBuilder.Build(
                    map,
                    [
                        new FafbVisualNeuronTypeRecord
                        {
                            RootId = 100,
                            Type = "A"
                        },

                        new FafbVisualNeuronTypeRecord
                        {
                            RootId = 100,
                            Type = "B"
                        }
                    ],
                    []));
    }
}