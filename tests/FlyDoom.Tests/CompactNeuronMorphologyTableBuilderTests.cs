using FlyDoom.Connectome.Build;
using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;

namespace FlyDoom.Tests;

public sealed class CompactNeuronMorphologyTableBuilderTests
{
    [Fact]
    public void Build_AlignsStatisticsWithNeuronIndices()
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
                new FafbCellStatsRecord
                {
                    RootId = 200,
                    LengthNm = 20,
                    AreaNm = 30,
                    SizeNm = 40
                },
                new FafbCellStatsRecord
                {
                    RootId = 100,
                    LengthNm = 1,
                    AreaNm = 2,
                    SizeNm = 3
                }
            };

        var table =
            CompactNeuronMorphologyTableBuilder.Build(
                records,
                map);

        Assert.Equal(
            1,
            table.GetLengthNm(0));

        Assert.Equal(
            20,
            table.GetLengthNm(1));
    }

    [Fact]
    public void Build_UsesNaNForMissingMeasurements()
    {
        var map =
            NeuronIndexMap.Create(
            [
                100
            ]);

        var records =
            new[]
            {
                new FafbCellStatsRecord
                {
                    RootId = 100
                }
            };

        var table =
            CompactNeuronMorphologyTableBuilder.Build(
                records,
                map);

        Assert.True(
            double.IsNaN(
                table.GetLengthNm(0)));
    }
}