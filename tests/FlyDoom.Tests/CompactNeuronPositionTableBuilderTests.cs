using FlyDoom.Connectome.Build;
using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;

namespace FlyDoom.Tests;

public sealed class CompactNeuronPositionTableBuilderTests
{
    [Fact]
    public void Build_AlignsPositionsWithNeuronIndices()
    {
        var map =
            NeuronIndexMap.Create(
            [
                100,
                200
            ]);

        var records =
            new[]
            {
                new FafbCoordinateRecord
                {
                    RootId = 200,
                    Position = "[4, 5, 6]",
                    SupervoxelId = 222
                },
                new FafbCoordinateRecord
                {
                    RootId = 100,
                    Position = "[1, 2, 3]",
                    SupervoxelId = 111
                }
            };

        var table =
            CompactNeuronPositionTableBuilder.Build(
                () => records,
                map);

        Assert.Equal(2, table.NeuronCount);
        Assert.Equal(2, table.PositionCount);

        Assert.Equal(
            (1d, 2d, 3d),
            table.GetPosition(0, 0));

        Assert.Equal(
            (4d, 5d, 6d),
            table.GetPosition(1, 0));

        Assert.Equal(
            111,
            table.GetSupervoxelId(0, 0));
    }

    [Fact]
    public void Build_PreservesMultiplePositionsForOneNeuron()
    {
        var map =
            NeuronIndexMap.Create(
            [
                100
            ]);

        var records =
            new[]
            {
                new FafbCoordinateRecord
                {
                    RootId = 100,
                    Position = "[1, 2, 3]",
                    SupervoxelId = 111
                },
                new FafbCoordinateRecord
                {
                    RootId = 100,
                    Position = "[4, 5, 6]",
                    SupervoxelId = 222
                }
            };

        var table =
            CompactNeuronPositionTableBuilder.Build(
                () => records,
                map);

        Assert.Equal(
            2,
            table.GetPositionCount(0));

        Assert.Equal(
            (1d, 2d, 3d),
            table.GetPosition(0, 0));

        Assert.Equal(
            (4d, 5d, 6d),
            table.GetPosition(0, 1));
    }
}