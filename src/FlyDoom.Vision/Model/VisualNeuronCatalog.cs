namespace FlyDoom.Vision.Model;

/// <summary>
/// Stores visual-system annotations aligned with compact simulation indices.
/// </summary>
public sealed class VisualNeuronCatalog
{
    private readonly string?[] _types;
    private readonly string?[] _families;
    private readonly string?[] _subsystems;
    private readonly string?[] _categories;
    private readonly string?[] _sides;

    private readonly string?[] _columnHemispheres;
    private readonly string?[] _columnTypes;
    private readonly string?[] _columnIds;

    private readonly double[] _columnX;
    private readonly double[] _columnY;
    private readonly double[] _columnP;
    private readonly double[] _columnQ;

    /// <summary>
    /// Gets the number of neurons represented by the catalogue.
    /// </summary>
    public int Count => _types.Length;

    public VisualNeuronCatalog(
        string?[] types,
        string?[] families,
        string?[] subsystems,
        string?[] categories,
        string?[] sides,
        string?[] columnHemispheres,
        string?[] columnTypes,
        string?[] columnIds,
        double[] columnX,
        double[] columnY,
        double[] columnP,
        double[] columnQ)
    {
        ArgumentNullException.ThrowIfNull(types);
        ArgumentNullException.ThrowIfNull(families);
        ArgumentNullException.ThrowIfNull(subsystems);
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(sides);
        ArgumentNullException.ThrowIfNull(columnHemispheres);
        ArgumentNullException.ThrowIfNull(columnTypes);
        ArgumentNullException.ThrowIfNull(columnIds);
        ArgumentNullException.ThrowIfNull(columnX);
        ArgumentNullException.ThrowIfNull(columnY);
        ArgumentNullException.ThrowIfNull(columnP);
        ArgumentNullException.ThrowIfNull(columnQ);

        var count = types.Length;

        if (families.Length != count ||
            subsystems.Length != count ||
            categories.Length != count ||
            sides.Length != count ||
            columnHemispheres.Length != count ||
            columnTypes.Length != count ||
            columnIds.Length != count ||
            columnX.Length != count ||
            columnY.Length != count ||
            columnP.Length != count ||
            columnQ.Length != count)
        {
            throw new ArgumentException(
                "All visual neuron arrays must have matching lengths.");
        }

        _types = types;
        _families = families;
        _subsystems = subsystems;
        _categories = categories;
        _sides = sides;

        _columnHemispheres = columnHemispheres;
        _columnTypes = columnTypes;
        _columnIds = columnIds;

        _columnX = columnX;
        _columnY = columnY;
        _columnP = columnP;
        _columnQ = columnQ;
    }

    /// <summary>
    /// Gets whether the neuron has visual-system annotation.
    /// </summary>
    public bool IsVisualNeuron(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);

        return _types[neuronIndex] is not null ||
               _families[neuronIndex] is not null ||
               _subsystems[neuronIndex] is not null ||
               _categories[neuronIndex] is not null;
    }

    /// <summary>
    /// Gets whether the neuron has a visual-column assignment.
    /// </summary>
    public bool HasColumnAssignment(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);

        return _columnIds[neuronIndex] is not null;
    }

    public string? GetType(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);
        return _types[neuronIndex];
    }

    public string? GetFamily(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);
        return _families[neuronIndex];
    }

    public string? GetSubsystem(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);
        return _subsystems[neuronIndex];
    }

    public string? GetCategory(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);
        return _categories[neuronIndex];
    }

    public string? GetSide(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);
        return _sides[neuronIndex];
    }

    public string? GetColumnHemisphere(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);
        return _columnHemispheres[neuronIndex];
    }

    public string? GetColumnType(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);
        return _columnTypes[neuronIndex];
    }

    public string? GetColumnId(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);
        return _columnIds[neuronIndex];
    }

    public double GetColumnX(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);
        return _columnX[neuronIndex];
    }

    public double GetColumnY(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);
        return _columnY[neuronIndex];
    }

    public double GetColumnP(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);
        return _columnP[neuronIndex];
    }

    public double GetColumnQ(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);
        return _columnQ[neuronIndex];
    }

    private void ValidateIndex(
        int neuronIndex)
    {
        if ((uint)neuronIndex >= (uint)Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(neuronIndex));
        }
    }
}