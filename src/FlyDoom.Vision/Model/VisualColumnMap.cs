namespace FlyDoom.Vision.Model;

/// <summary>
/// Represents the spatial visual columns of both optic lobes.
/// </summary>
public sealed class VisualColumnMap
{
    private readonly VisualColumn[] _columns;

    /// <summary>
    /// Gets the number of spatial visual columns.
    /// </summary>
    public int Count => _columns.Length;

    /// <summary>
    /// Gets all spatial visual columns.
    /// </summary>
    public IReadOnlyList<VisualColumn> Columns =>
        _columns;

    internal VisualColumnMap(
        VisualColumn[] columns)
    {
        ArgumentNullException.ThrowIfNull(
            columns);

        _columns =
            columns;
    }

    /// <summary>
    /// Gets a visual column by compact column index.
    /// </summary>
    public VisualColumn GetColumn(
        int columnIndex)
    {
        if ((uint)columnIndex >=
            (uint)_columns.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columnIndex));
        }

        return _columns[columnIndex];
    }
}

/// <summary>
/// Represents one spatial visual column in one optic lobe.
/// </summary>
public sealed class VisualColumn
{
    private readonly int[] _neuronIndices;

    /// <summary>
    /// Gets the hemisphere containing the column.
    /// </summary>
    public string Hemisphere { get; }

    /// <summary>
    /// Gets the source dataset's column identifier.
    /// </summary>
    public string ColumnId { get; }

    /// <summary>
    /// Gets the compact indices of neurons assigned to this column.
    /// </summary>
    public IReadOnlyList<int> NeuronIndices =>
        _neuronIndices;

    /// <summary>
    /// Gets the number of neurons assigned to this column.
    /// </summary>
    public int NeuronCount =>
        _neuronIndices.Length;

    /// <summary>
    /// Gets the mean X coordinate of assigned neurons.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the mean Y coordinate of assigned neurons.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Gets the mean P coordinate of assigned neurons.
    /// </summary>
    public double P { get; }

    /// <summary>
    /// Gets the mean Q coordinate of assigned neurons.
    /// </summary>
    public double Q { get; }

    internal VisualColumn(
        string hemisphere,
        string columnId,
        int[] neuronIndices,
        double x,
        double y,
        double p,
        double q)
    {
        Hemisphere =
            hemisphere;

        ColumnId =
            columnId;

        _neuronIndices =
            neuronIndices;

        X = x;
        Y = y;
        P = p;
        Q = q;
    }
}