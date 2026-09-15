using FlyDoom.Connectome.Build;
using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;

namespace FlyDoom.Tests;

public sealed class CompactNeuronIdentityTableBuilderTests
{
    [Fact]
    public void Build_AlignsIdentityDataWithNeuronIndices()
    {
        var neuronIndexMap =
            NeuronIndexMap.Create(
            [
                100,
                200
            ]);

        var names =
            new[]
            {
                new FafbNameRecord
                {
                    RootId = 200,
                    Name = "Neuron B",
                    Group = "Group B"
                },
                new FafbNameRecord
                {
                    RootId = 100,
                    Name = "Neuron A",
                    Group = "Group A"
                }
            };

        var classifications =
            new[]
            {
                new FafbClassificationRecord
                {
                    RootId = 100,
                    Flow = "intrinsic",
                    SuperClass = "central",
                    Class = "Kenyon cell",
                    SubClass = "KC",
                    Hemilineage = "lineage-a",
                    Side = "left"
                }
            };

        var cellTypes =
            new[]
            {
                new FafbCellTypeRecord
                {
                    RootId = 100,
                    PrimaryType = "KCg-m",
                    AdditionalTypes = "example"
                }
            };

        var table =
            CompactNeuronIdentityTableBuilder.Build(
                neuronIndexMap,
                names,
                classifications,
                cellTypes);

        Assert.Equal(2, table.Count);

        Assert.Equal(
            "Neuron A",
            table.GetName(0));

        Assert.Equal(
            "Neuron B",
            table.GetName(1));

        Assert.Equal(
            "Group A",
            table.GetGroup(0));

        Assert.Equal(
            "Kenyon cell",
            table.GetClass(0));

        Assert.Equal(
            "KCg-m",
            table.GetPrimaryType(0));

        Assert.Equal(
            "left",
            table.GetSide(0));

        Assert.Null(
            table.GetClass(1));

        Assert.Null(
            table.GetPrimaryType(1));
    }

    [Fact]
    public void Build_NormalisesBlankValuesToNull()
    {
        var neuronIndexMap =
            NeuronIndexMap.Create(
            [
                100
            ]);

        var names =
            new[]
            {
                new FafbNameRecord
                {
                    RootId = 100,
                    Name = "   ",
                    Group = null
                }
            };

        var table =
            CompactNeuronIdentityTableBuilder.Build(
                neuronIndexMap,
                names,
                Array.Empty<FafbClassificationRecord>(),
                Array.Empty<FafbCellTypeRecord>());

        Assert.Null(table.GetName(0));
        Assert.Null(table.GetGroup(0));
    }

    [Fact]
    public void Build_RejectsUnknownNeuron()
    {
        var neuronIndexMap =
            NeuronIndexMap.Create(
            [
                100
            ]);

        var names =
            new[]
            {
                new FafbNameRecord
                {
                    RootId = 999,
                    Name = "Mystery neuron"
                }
            };

        Assert.Throws<InvalidDataException>(
            () => CompactNeuronIdentityTableBuilder.Build(
                neuronIndexMap,
                names,
                Array.Empty<FafbClassificationRecord>(),
                Array.Empty<FafbCellTypeRecord>()));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void GetIdentity_RejectsInvalidIndex(
        int neuronIndex)
    {
        var neuronIndexMap =
            NeuronIndexMap.Create(
            [
                100
            ]);

        var table =
            CompactNeuronIdentityTableBuilder.Build(
                neuronIndexMap,
                Array.Empty<FafbNameRecord>(),
                Array.Empty<FafbClassificationRecord>(),
                Array.Empty<FafbCellTypeRecord>());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => table.GetName(neuronIndex));
    }
}