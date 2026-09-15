using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;

namespace FlyDoom.Connectome.Build;

/// <summary>
/// Builds compact morphological metadata aligned with simulation indices.
/// </summary>
public static class CompactNeuronMorphologyTableBuilder
{
    public static CompactNeuronMorphologyTable Build(
        IEnumerable<FafbCellStatsRecord> records,
        NeuronIndexMap neuronIndexMap)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(neuronIndexMap);

        var count =
            neuronIndexMap.Count;

        var lengths =
            new double[count];

        var areas =
            new double[count];

        var sizes =
            new double[count];

        Array.Fill(lengths, double.NaN);
        Array.Fill(areas, double.NaN);
        Array.Fill(sizes, double.NaN);

        var seen =
            new bool[count];

        foreach (var record in records)
        {
            if (!neuronIndexMap.TryGetIndex(
                    record.RootId,
                    out var index))
            {
                throw new InvalidDataException(
                    $"Cell statistics reference unknown neuron root ID: " +
                    $"{record.RootId}");
            }

            if (seen[index])
            {
                throw new InvalidDataException(
                    $"Duplicate cell statistics for neuron root ID: " +
                    $"{record.RootId}");
            }

            seen[index] = true;

            lengths[index] =
                record.LengthNm ?? double.NaN;

            areas[index] =
                record.AreaNm ?? double.NaN;

            sizes[index] =
                record.SizeNm ?? double.NaN;
        }

        return new CompactNeuronMorphologyTable(
            lengths,
            areas,
            sizes);
    }
}