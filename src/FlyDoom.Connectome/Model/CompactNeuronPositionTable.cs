namespace FlyDoom.Connectome.Model;

/// <summary>
/// Stores one or more representative spatial positions for each neuron.
/// </summary>
/// <remarks>
/// Position records are grouped by compact neuron index. The offset array
/// identifies the range of coordinate records belonging to each neuron.
/// </remarks>
public sealed class CompactNeuronPositionTable
{
    private readonly int[] _offsets;
    private readonly double[] _x;
    private readonly double[] _y;
    private readonly double[] _z;
    private readonly long?[] _supervoxelIds;

    /// <summary>
    /// Gets the number of neurons represented by the table.
    /// </summary>
    public int NeuronCount => _offsets.Length - 1;

    /// <summary>
    /// Gets the total number of coordinate records.
    /// </summary>
    public int PositionCount => _x.Length;

    public CompactNeuronPositionTable(
        int[] offsets,
        double[] x,
        double[] y,
        double[] z,
        long?[] supervoxelIds)
    {
        ArgumentNullException.ThrowIfNull(offsets);
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);
        ArgumentNullException.ThrowIfNull(z);
        ArgumentNullException.ThrowIfNull(supervoxelIds);

        if (offsets.Length == 0)
        {
            throw new ArgumentException(
                "Offsets must contain at least one entry.",
                nameof(offsets));
        }

        if (y.Length != x.Length ||
            z.Length != x.Length ||
            supervoxelIds.Length != x.Length)
        {
            throw new ArgumentException(
                "All position arrays must have matching lengths.");
        }

        if (offsets[^1] != x.Length)
        {
            throw new ArgumentException(
                "The final offset must equal the position count.",
                nameof(offsets));
        }

        _offsets = offsets;
        _x = x;
        _y = y;
        _z = z;
        _supervoxelIds = supervoxelIds;
    }

    /// <summary>
    /// Gets the number of stored positions for a neuron.
    /// </summary>
    public int GetPositionCount(int neuronIndex)
    {
        var (start, length) =
            GetRange(neuronIndex);

        return length;
    }

    /// <summary>
    /// Gets the position at the specified index within a neuron's
    /// coordinate records.
    /// </summary>
    public (double X, double Y, double Z) GetPosition(
        int neuronIndex,
        int positionIndex)
    {
        var (start, length) =
            GetRange(neuronIndex);

        if ((uint)positionIndex >= (uint)length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(positionIndex));
        }

        var absoluteIndex =
            start + positionIndex;

        return (
            _x[absoluteIndex],
            _y[absoluteIndex],
            _z[absoluteIndex]);
    }

    /// <summary>
    /// Gets the supervoxel ID associated with a stored position.
    /// </summary>
    public long? GetSupervoxelId(
        int neuronIndex,
        int positionIndex)
    {
        var (start, length) =
            GetRange(neuronIndex);

        if ((uint)positionIndex >= (uint)length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(positionIndex));
        }

        return _supervoxelIds[
            start + positionIndex];
    }

    private (int Start, int Length) GetRange(
        int neuronIndex)
    {
        if ((uint)neuronIndex >= (uint)NeuronCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(neuronIndex));
        }

        var start =
            _offsets[neuronIndex];

        var end =
            _offsets[neuronIndex + 1];

        return (
            start,
            end - start);
    }
}