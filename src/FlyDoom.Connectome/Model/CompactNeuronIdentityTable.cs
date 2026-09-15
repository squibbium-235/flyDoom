namespace FlyDoom.Connectome.Model;

/// <summary>
/// Stores descriptive neuron metadata aligned with compact simulation indices.
/// </summary>
public sealed class CompactNeuronIdentityTable
{
    private readonly string?[] _names;
    private readonly string?[] _groups;
    private readonly string?[] _primaryTypes;
    private readonly string?[] _additionalTypes;
    private readonly string?[] _flows;
    private readonly string?[] _superClasses;
    private readonly string?[] _classes;
    private readonly string?[] _subClasses;
    private readonly string?[] _hemilineages;
    private readonly string?[] _sides;
    private readonly string?[] _nerves;

    /// <summary>
    /// Gets the number of neurons represented by the table.
    /// </summary>
    public int Count => _names.Length;

    public CompactNeuronIdentityTable(
        string?[] names,
        string?[] groups,
        string?[] primaryTypes,
        string?[] additionalTypes,
        string?[] flows,
        string?[] superClasses,
        string?[] classes,
        string?[] subClasses,
        string?[] hemilineages,
        string?[] sides,
        string?[] nerves)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(primaryTypes);
        ArgumentNullException.ThrowIfNull(additionalTypes);
        ArgumentNullException.ThrowIfNull(flows);
        ArgumentNullException.ThrowIfNull(superClasses);
        ArgumentNullException.ThrowIfNull(classes);
        ArgumentNullException.ThrowIfNull(subClasses);
        ArgumentNullException.ThrowIfNull(hemilineages);
        ArgumentNullException.ThrowIfNull(sides);
        ArgumentNullException.ThrowIfNull(nerves);

        var count = names.Length;

        if (groups.Length != count ||
            primaryTypes.Length != count ||
            additionalTypes.Length != count ||
            flows.Length != count ||
            superClasses.Length != count ||
            classes.Length != count ||
            subClasses.Length != count ||
            hemilineages.Length != count ||
            sides.Length != count ||
            nerves.Length != count)
        {
            throw new ArgumentException(
                "All neuron identity arrays must have matching lengths.");
        }

        _names = names;
        _groups = groups;
        _primaryTypes = primaryTypes;
        _additionalTypes = additionalTypes;
        _flows = flows;
        _superClasses = superClasses;
        _classes = classes;
        _subClasses = subClasses;
        _hemilineages = hemilineages;
        _sides = sides;
        _nerves = nerves;
    }

    public string? GetName(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _names[neuronIndex];
    }

    public string? GetGroup(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _groups[neuronIndex];
    }

    public string? GetPrimaryType(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _primaryTypes[neuronIndex];
    }

    public string? GetAdditionalTypes(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _additionalTypes[neuronIndex];
    }

    public string? GetFlow(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _flows[neuronIndex];
    }

    public string? GetSuperClass(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _superClasses[neuronIndex];
    }

    public string? GetClass(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _classes[neuronIndex];
    }

    public string? GetSubClass(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _subClasses[neuronIndex];
    }

    public string? GetHemilineage(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _hemilineages[neuronIndex];
    }

    public string? GetSide(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _sides[neuronIndex];
    }

    public string? GetNerve(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _nerves[neuronIndex];
    }

    private void ValidateNeuronIndex(int neuronIndex)
    {
        if ((uint)neuronIndex >= (uint)Count)
        {
            throw new ArgumentOutOfRangeException(nameof(neuronIndex));
        }
    }
}