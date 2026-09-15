namespace FlyDoom.Connectome.Model;

/// <summary>
/// Stores connectome connectivity in compact contiguous arrays optimised
/// for simulation.
/// </summary>
/// <remarks>
/// Connections are grouped by presynaptic neuron. The outgoing offset array
/// identifies the range of connection entries belonging to each neuron.
/// Neuropil names are stored once and referenced by compact integer indices.
/// </remarks>
public sealed class CompactConnectome
{
    private readonly int[] _outgoingOffsets;
    private readonly int[] _postsynapticIndices;
    private readonly int[] _synapseCounts;
    private readonly ushort[] _neuropilIndices;
    private readonly string[] _neuropilNames;

    /// <summary>
    /// Gets the number of neurons represented by the connectome.
    /// </summary>
    public int NeuronCount => _outgoingOffsets.Length - 1;

    /// <summary>
    /// Gets the number of aggregated connection entries.
    /// </summary>
    public int ConnectionCount => _postsynapticIndices.Length;

    /// <summary>
    /// Gets the number of distinct neuropils represented by the connectome.
    /// </summary>
    public int NeuropilCount => _neuropilNames.Length;

    /// <summary>
    /// Initialises a compact connectome from pre-built connection arrays.
    /// </summary>
    public CompactConnectome(
        int[] outgoingOffsets,
        int[] postsynapticIndices,
        int[] synapseCounts,
        ushort[] neuropilIndices,
        string[] neuropilNames)
    {
        ArgumentNullException.ThrowIfNull(outgoingOffsets);
        ArgumentNullException.ThrowIfNull(postsynapticIndices);
        ArgumentNullException.ThrowIfNull(synapseCounts);
        ArgumentNullException.ThrowIfNull(neuropilIndices);
        ArgumentNullException.ThrowIfNull(neuropilNames);

        if (outgoingOffsets.Length == 0)
        {
            throw new ArgumentException(
                "Outgoing offsets must contain at least one entry.",
                nameof(outgoingOffsets));
        }

        if (postsynapticIndices.Length != synapseCounts.Length ||
            postsynapticIndices.Length != neuropilIndices.Length)
        {
            throw new ArgumentException(
                "Connection arrays must have matching lengths.");
        }

        if (outgoingOffsets[^1] != postsynapticIndices.Length)
        {
            throw new ArgumentException(
                "The final outgoing offset must equal the connection count.",
                nameof(outgoingOffsets));
        }

        foreach (var neuropilIndex in neuropilIndices)
        {
            if (neuropilIndex >= neuropilNames.Length)
            {
                throw new ArgumentException(
                    $"Neuropil index {neuropilIndex} has no corresponding name.",
                    nameof(neuropilIndices));
            }
        }

        _outgoingOffsets = outgoingOffsets;
        _postsynapticIndices = postsynapticIndices;
        _synapseCounts = synapseCounts;
        _neuropilIndices = neuropilIndices;
        _neuropilNames = neuropilNames;
    }

    /// <summary>
    /// Gets the postsynaptic neuron indices targeted by the specified neuron.
    /// </summary>
    public ReadOnlySpan<int> GetPostsynapticIndices(
        int presynapticIndex)
    {
        var (start, length) =
            GetOutgoingRange(presynapticIndex);

        return _postsynapticIndices.AsSpan(start, length);
    }

    /// <summary>
    /// Gets the anatomical synapse counts for the specified neuron's
    /// outgoing connections.
    /// </summary>
    public ReadOnlySpan<int> GetSynapseCounts(
        int presynapticIndex)
    {
        var (start, length) =
            GetOutgoingRange(presynapticIndex);

        return _synapseCounts.AsSpan(start, length);
    }

    /// <summary>
    /// Gets the neuropil indices for the specified neuron's outgoing
    /// connections.
    /// </summary>
    public ReadOnlySpan<ushort> GetNeuropilIndices(
        int presynapticIndex)
    {
        var (start, length) =
            GetOutgoingRange(presynapticIndex);

        return _neuropilIndices.AsSpan(start, length);
    }

    /// <summary>
    /// Gets the name associated with a compact neuropil index.
    /// </summary>
    public string GetNeuropilName(
        ushort neuropilIndex)
    {
        if (neuropilIndex >= _neuropilNames.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(neuropilIndex));
        }

        return _neuropilNames[neuropilIndex];
    }

    /// <summary>
    /// Gets the range containing a neuron's outgoing connections.
    /// </summary>
    private (int Start, int Length) GetOutgoingRange(
        int presynapticIndex)
    {
        if ((uint)presynapticIndex >= (uint)NeuronCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(presynapticIndex));
        }

        var start =
            _outgoingOffsets[presynapticIndex];

        var end =
            _outgoingOffsets[presynapticIndex + 1];

        return (start, end - start);
    }
}