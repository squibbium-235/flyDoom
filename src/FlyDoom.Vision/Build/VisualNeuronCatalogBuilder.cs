using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;
using FlyDoom.Vision.Model;

namespace FlyDoom.Vision.Build;

/// <summary>
/// Builds visual-system metadata aligned with compact simulation indices.
/// </summary>
public static class VisualNeuronCatalogBuilder
{
    public static VisualNeuronCatalog Build(
        NeuronIndexMap neuronIndexMap,
        IEnumerable<FafbVisualNeuronTypeRecord> visualNeurons,
        IEnumerable<FafbColumnAssignmentRecord> columnAssignments)
    {
        ArgumentNullException.ThrowIfNull(
            neuronIndexMap);

        ArgumentNullException.ThrowIfNull(
            visualNeurons);

        ArgumentNullException.ThrowIfNull(
            columnAssignments);

        var count =
            neuronIndexMap.Count;

        var types =
            new string?[count];

        var families =
            new string?[count];

        var subsystems =
            new string?[count];

        var categories =
            new string?[count];

        var sides =
            new string?[count];

        var columnHemispheres =
            new string?[count];

        var columnTypes =
            new string?[count];

        var columnIds =
            new string?[count];

        var columnX =
            CreateNaNArray(count);

        var columnY =
            CreateNaNArray(count);

        var columnP =
            CreateNaNArray(count);

        var columnQ =
            CreateNaNArray(count);

        var seenVisualAnnotation =
            new bool[count];

        var seenColumnAssignment =
            new bool[count];

        foreach (var record in visualNeurons)
        {
            var neuronIndex =
                GetNeuronIndex(
                    neuronIndexMap,
                    record.RootId,
                    "Visual annotation");

            if (seenVisualAnnotation[neuronIndex])
            {
                throw new InvalidDataException(
                    $"Duplicate visual annotation for neuron " +
                    $"root ID {record.RootId}.");
            }

            seenVisualAnnotation[neuronIndex] =
                true;

            types[neuronIndex] =
                Normalise(record.Type);

            families[neuronIndex] =
                Normalise(record.Family);

            subsystems[neuronIndex] =
                Normalise(record.Subsystem);

            categories[neuronIndex] =
                Normalise(record.Category);

            sides[neuronIndex] =
                Normalise(record.Side);
        }

        foreach (var record in columnAssignments)
        {
            var neuronIndex =
                GetNeuronIndex(
                    neuronIndexMap,
                    record.RootId,
                    "Visual column assignment");

            if (seenColumnAssignment[neuronIndex])
            {
                throw new InvalidDataException(
                    $"Duplicate visual column assignment for neuron " +
                    $"root ID {record.RootId}.");
            }

            seenColumnAssignment[neuronIndex] =
                true;

            columnHemispheres[neuronIndex] =
                Normalise(record.Hemisphere);

            columnTypes[neuronIndex] =
                Normalise(record.Type);

            columnIds[neuronIndex] =
                Normalise(record.ColumnId);

            columnX[neuronIndex] =
                record.X ?? double.NaN;

            columnY[neuronIndex] =
                record.Y ?? double.NaN;

            columnP[neuronIndex] =
                record.P ?? double.NaN;

            columnQ[neuronIndex] =
                record.Q ?? double.NaN;
        }

        return new VisualNeuronCatalog(
            types,
            families,
            subsystems,
            categories,
            sides,
            columnHemispheres,
            columnTypes,
            columnIds,
            columnX,
            columnY,
            columnP,
            columnQ);
    }

    private static int GetNeuronIndex(
        NeuronIndexMap neuronIndexMap,
        long rootId,
        string sourceName)
    {
        if (!neuronIndexMap.TryGetIndex(
                rootId,
                out var neuronIndex))
        {
            throw new InvalidDataException(
                $"{sourceName} references unknown neuron root ID " +
                $"{rootId}.");
        }

        return neuronIndex;
    }

    private static double[] CreateNaNArray(
        int count)
    {
        var values =
            new double[count];

        Array.Fill(
            values,
            double.NaN);

        return values;
    }

    private static string? Normalise(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}