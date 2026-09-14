namespace FlyDoom.Connectome.Model;

/// <summary>
/// Maps dataset neuron IDs to compact, zero-based sim indices
/// </summary>
/// <remarks>
/// FlyWire rood IDs are large, sparce identifiers, meaning they can't be used as array indices
/// This class assignes every neuron a contiguous integer index while preserving a mapping back to its original ID
/// </remarks>
public sealed class NeuronIndexMap
{
    private readonly Dictionary<long, int> _rootIdToIndex;
    private readonly long[] _indexToRootId;

    /// <summary>
    /// Gets the number of neurons contained in the mapping.
    /// </summary>
    public int Count => _indexToRootId.Length;

    /// <summary>
    /// Creates a new neuron index mapping.
    /// </summary>
    /// <param name="rootIdToIndex">
    /// Mapping from FlyWire root IDs to compact sim indices
    /// </param>
    /// <param name="indexToRootId">
    /// Array mapping compact sim indices back to FlyWire root IDs.
    /// </param>
    private NeuronIndexMap(
        Dictionary<long, int> rootIdToIndex,
        long[] indexToRootId)
    {
        _rootIdToIndex = rootIdToIndex;
        _indexToRootId = indexToRootId;
    }

    /// <summary>
    /// Creates a compact index mapping from a sequence of neuron root IDs.
    /// </summary>
    /// <param name="rootIds">
    /// The original neuron identifiers supplied by the connectome datasaet.
    /// </param>
    /// <returns>
    /// A mapping between dataset root IDs and compact sim indices.
    /// </returns>
    /// <exception cref="InvalidDataException">
    /// Thrown if the dataset contains the same neuron root ID more than once.
    /// </exception>
    public static NeuronIndexMap Create(IEnumerable<long> rootIds)
    {
        var rootIdToIndex = new Dictionary<long, int>();
        var indexToRootId = new List<long>();

        foreach(var rootID in rootIds)
        {
            // The current number of mapped neurons becomes the next avaliable
            // compact index: 0, 1, 2, 3, ...
            var index = indexToRootId.Count;

            // Duplicate neuron IDs indicate inconsitent source data
            // these should be detected here rather that breaking the simulation
            if(!rootIdToIndex.TryAdd(rootID, index))
            {
                throw new InvalidDataException(
                    $"Duplicate neuron root ID found: {rootID}");
            }

            indexToRootId.Add(rootID);
        }

        return new NeuronIndexMap(
            rootIdToIndex,
            indexToRootId.ToArray());
    }

    /// <summary>
    /// Attempts to retrieve the sim index for a FlyWire rood ID.
    /// </summary>
    public bool TryGetIndex(long rootId, out int index)
    {
        return _rootIdToIndex.TryGetValue(rootId, out index);
    }

    /// <summary>
    /// Gets the compact simulation index for a FlyWire root ID>
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown if the root ID is not present in the mapping.
    /// </exception>
    public int GetIndex(long rootId)
    {
        return _rootIdToIndex[rootId];
    }

    /// <summary>
    /// Gets the original FlyWire root ID associated with a sim index.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the supplied simulation index does not exist.
    /// </exception>
    public long GetRootId(int index)
    {
        if ((uint)index >= (uint)_indexToRootId.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _indexToRootId[index];
    }
}