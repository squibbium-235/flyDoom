using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;

namespace FlyDoom.Connectome.Build;

/// <summary>
/// Builds descriptive neuron metadata aligned with simulation neuron indices.
/// </summary>
public static class CompactNeuronIdentityTableBuilder
{
    public static CompactNeuronIdentityTable Build(
        NeuronIndexMap neuronIndexMap,
        IEnumerable<FafbNameRecord> names,
        IEnumerable<FafbClassificationRecord> classifications,
        IEnumerable<FafbCellTypeRecord> cellTypes)
    {
        ArgumentNullException.ThrowIfNull(neuronIndexMap);
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(classifications);
        ArgumentNullException.ThrowIfNull(cellTypes);

        var count = neuronIndexMap.Count;

        var neuronNames = new string?[count];
        var groups = new string?[count];
        var primaryTypes = new string?[count];
        var additionalTypes = new string?[count];

        var flows = new string?[count];
        var superClasses = new string?[count];
        var classes = new string?[count];
        var subClasses = new string?[count];
        var hemilineages = new string?[count];
        var sides = new string?[count];
        var nerves = new string?[count];

        foreach (var record in names)
        {
            var index =
                GetNeuronIndex(
                    neuronIndexMap,
                    record.RootId,
                    "name");

            neuronNames[index] =
                Normalise(record.Name);

            groups[index] =
                Normalise(record.Group);
        }

        foreach (var record in classifications)
        {
            var index =
                GetNeuronIndex(
                    neuronIndexMap,
                    record.RootId,
                    "classification");

            flows[index] =
                Normalise(record.Flow);

            superClasses[index] =
                Normalise(record.SuperClass);

            classes[index] =
                Normalise(record.Class);

            subClasses[index] =
                Normalise(record.SubClass);

            hemilineages[index] =
                Normalise(record.Hemilineage);

            sides[index] =
                Normalise(record.Side);

            nerves[index] =
                Normalise(record.Nerve);
        }

        foreach (var record in cellTypes)
        {
            var index =
                GetNeuronIndex(
                    neuronIndexMap,
                    record.RootId,
                    "cell type");

            primaryTypes[index] =
                Normalise(record.PrimaryType);

            additionalTypes[index] =
                Normalise(record.AdditionalTypes);
        }

        return new CompactNeuronIdentityTable(
            neuronNames,
            groups,
            primaryTypes,
            additionalTypes,
            flows,
            superClasses,
            classes,
            subClasses,
            hemilineages,
            sides,
            nerves);
    }

    private static int GetNeuronIndex(
        NeuronIndexMap neuronIndexMap,
        long rootId,
        string source)
    {
        if (!neuronIndexMap.TryGetIndex(
                rootId,
                out var index))
        {
            throw new InvalidDataException(
                $"{source} record references unknown neuron root ID: {rootId}");
        }

        return index;
    }

    private static string? Normalise(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}