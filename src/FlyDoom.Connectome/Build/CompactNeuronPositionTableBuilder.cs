using FlyDoom.Connectome.Import;
using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;

namespace FlyDoom.Connectome.Build;

/// <summary>
/// Builds spatial neuron metadata aligned with compact simulation indices.
/// </summary>
public static class CompactNeuronPositionTableBuilder
{
    public static CompactNeuronPositionTable Build(
        Func<IEnumerable<FafbCoordinateRecord>> coordinateSource,
        NeuronIndexMap neuronIndexMap)
    {
        ArgumentNullException.ThrowIfNull(coordinateSource);
        ArgumentNullException.ThrowIfNull(neuronIndexMap);

        var positionCounts =
            new int[neuronIndexMap.Count];

        var totalPositions = 0;

        // First pass: count how many coordinate records belong to each neuron.
        foreach (var coordinate in coordinateSource())
        {
            var index =
                GetNeuronIndex(
                    coordinate,
                    neuronIndexMap);

            positionCounts[index] =
                checked(positionCounts[index] + 1);

            totalPositions =
                checked(totalPositions + 1);
        }

        var offsets =
            BuildOffsets(positionCounts);

        var x =
            new double[totalPositions];

        var y =
            new double[totalPositions];

        var z =
            new double[totalPositions];

        var supervoxelIds =
            new long?[totalPositions];

        var writePositions =
            new int[neuronIndexMap.Count];

        Array.Copy(
            offsets,
            writePositions,
            writePositions.Length);

        var writtenPositions = 0;

        // Second pass: place every coordinate into its neuron's contiguous
        // section without discarding duplicate root IDs.
        foreach (var coordinate in coordinateSource())
        {
            var neuronIndex =
                GetNeuronIndex(
                    coordinate,
                    neuronIndexMap);

            if (string.IsNullOrWhiteSpace(
                    coordinate.Position))
            {
                throw new InvalidDataException(
                    $"Coordinate record for root ID " +
                    $"{coordinate.RootId} has no position.");
            }

            var position =
                FafbPositionParser.Parse(
                    coordinate.Position);

            var writePosition =
                writePositions[neuronIndex];

            var end =
                offsets[neuronIndex + 1];

            if (writePosition >= end)
            {
                throw new InvalidOperationException(
                    "Coordinate source changed between build passes.");
            }

            x[writePosition] =
                position.X;

            y[writePosition] =
                position.Y;

            z[writePosition] =
                position.Z;

            supervoxelIds[writePosition] =
                coordinate.SupervoxelId;

            writePositions[neuronIndex]++;
            writtenPositions++;
        }

        if (writtenPositions != totalPositions)
        {
            throw new InvalidOperationException(
                "Coordinate source changed between build passes.");
        }

        return new CompactNeuronPositionTable(
            offsets,
            x,
            y,
            z,
            supervoxelIds);
    }

    private static int GetNeuronIndex(
        FafbCoordinateRecord coordinate,
        NeuronIndexMap neuronIndexMap)
    {
        if (!neuronIndexMap.TryGetIndex(
                coordinate.RootId,
                out var index))
        {
            throw new InvalidDataException(
                $"Coordinate references unknown neuron root ID: " +
                $"{coordinate.RootId}");
        }

        return index;
    }

    private static int[] BuildOffsets(
        int[] counts)
    {
        var offsets =
            new int[counts.Length + 1];

        for (var i = 0;
             i < counts.Length;
             i++)
        {
            offsets[i + 1] =
                checked(
                    offsets[i] +
                    counts[i]);
        }

        return offsets;
    }
}