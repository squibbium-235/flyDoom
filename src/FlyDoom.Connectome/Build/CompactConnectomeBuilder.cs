using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;
using FlyDoom.Core.Biology;
using FlyDoom.Connectome.Import;

namespace FlyDoom.Connectome.Build;

/// <summary>
/// Builds compact simulation connectivity from FAFB connection records.
/// </summary>
public static class CompactConnectomeBuilder
{
    /// <summary>
    /// Builds a compact connectome from a replayable source of FAFB
    /// connection records.
    /// </summary>
    /// <param name="neuronIndexMap">
    /// Mapping from FlyWire root IDs to compact simulation indices.
    /// </param>
    /// <param name="connectionSource">
    /// Function that returns a fresh enumeration of connection records.
    /// The source is enumerated twice while building the connectome.
    /// </param>
    /// <returns>
    /// A compact representation of the supplied connectivity.
    /// </returns>
    public static CompactConnectome Build(
        NeuronIndexMap neuronIndexMap,
        Func<IEnumerable<FafbConnectionRecord>> connectionSource)
    {
        ArgumentNullException.ThrowIfNull(neuronIndexMap);
        ArgumentNullException.ThrowIfNull(connectionSource);

        var outgoingCounts =
            new int[neuronIndexMap.Count];

        var connectionCount = 0;

        // Store each neuropil name once and reference it using a compact
        // integer index for every connection occurring within that neuropil.
        var neuropilToIndex =
            new Dictionary<string, ushort>(
                StringComparer.Ordinal);

        var neuropilNames =
            new List<string>();

        // First pass: determine how much contiguous space each neuron needs
        // and discover all neuropils represented by the dataset.
        foreach (var connection in connectionSource())
        {
            var (presynapticIndex, _) =
                ValidateAndMapConnection(
                    connection,
                    neuronIndexMap);

            outgoingCounts[presynapticIndex] =
                checked(
                    outgoingCounts[presynapticIndex] + 1);

            connectionCount =
                checked(connectionCount + 1);

            if (string.IsNullOrWhiteSpace(
                    connection.Neuropil))
            {
                throw new InvalidDataException(
                    "Connection has no neuropil.");
            }

            if (!neuropilToIndex.ContainsKey(
                    connection.Neuropil))
            {
                if (neuropilNames.Count >
                    ushort.MaxValue)
                {
                    throw new InvalidDataException(
                        "Too many distinct neuropils to represent with ushort indices.");
                }

                var neuropilIndex =
                    checked(
                        (ushort)neuropilNames.Count);

                neuropilToIndex.Add(
                    connection.Neuropil,
                    neuropilIndex);

                neuropilNames.Add(
                    connection.Neuropil);
            }
        }

        var outgoingOffsets =
            BuildOutgoingOffsets(outgoingCounts);

        var postsynapticIndices =
            new int[connectionCount];

        var synapseCounts =
            new int[connectionCount];

        var neuropilIndices =
            new ushort[connectionCount];

        var neurotransmitterTypes =
            new NeurotransmitterType[
                connectionCount];

        // Each neuron writes into its own section of the connection arrays.
        var writePositions =
            new int[neuronIndexMap.Count];

        Array.Copy(
            outgoingOffsets,
            writePositions,
            writePositions.Length);

        var writtenConnections = 0;

        // Second pass: place each connection into its presynaptic neuron's
        // contiguous section.
        foreach (var connection in connectionSource())
        {
            var (presynapticIndex, postsynapticIndex) =
                ValidateAndMapConnection(
                    connection,
                    neuronIndexMap);

            var writePosition =
                writePositions[presynapticIndex];

            var end =
                outgoingOffsets[
                    presynapticIndex + 1];

            // If the source changes between passes, the array layout would
            // no longer match the counts calculated during the first pass.
            if (writePosition >= end)
            {
                throw new InvalidOperationException(
                    "Connection source changed between build passes.");
            }

            postsynapticIndices[writePosition] =
                postsynapticIndex;

            synapseCounts[writePosition] =
                connection.SynapseCount;

            if (string.IsNullOrWhiteSpace(
                    connection.Neuropil) ||
                !neuropilToIndex.TryGetValue(
                    connection.Neuropil,
                    out var neuropilIndex))
            {
                throw new InvalidOperationException(
                    "Connection source changed between build passes.");
            }

            neuropilIndices[writePosition] =
                neuropilIndex;

            neurotransmitterTypes[writePosition] =
               FafbNeurotransmitterParser.Parse(
                    connection.NeurotransmitterType);

            writePositions[presynapticIndex]++;

            writtenConnections++;
        }

        if (writtenConnections != connectionCount)
        {
            throw new InvalidOperationException(
                "Connection source changed between build passes.");
        }

        return new CompactConnectome(
            outgoingOffsets,
            postsynapticIndices,
            synapseCounts,
            neuropilIndices,
            neurotransmitterTypes,
            neuropilNames.ToArray());
    }

    /// <summary>
    /// Converts outgoing connection counts into cumulative array offsets.
    /// </summary>
    private static int[] BuildOutgoingOffsets(
        int[] outgoingCounts)
    {
        var offsets =
            new int[outgoingCounts.Length + 1];

        for (var i = 0;
             i < outgoingCounts.Length;
             i++)
        {
            offsets[i + 1] =
                checked(
                    offsets[i] +
                    outgoingCounts[i]);
        }

        return offsets;
    }

    /// <summary>
    /// Validates a connection record and maps its FlyWire root IDs to
    /// compact simulation indices.
    /// </summary>
    private static (
        int PresynapticIndex,
        int PostsynapticIndex)
        ValidateAndMapConnection(
            FafbConnectionRecord connection,
            NeuronIndexMap neuronIndexMap)
    {
        if (connection.SynapseCount <= 0)
        {
            throw new InvalidDataException(
                $"Connection has invalid synapse count: " +
                $"{connection.SynapseCount}");
        }

        if (!neuronIndexMap.TryGetIndex(
                connection.PresynapticRootId,
                out var presynapticIndex))
        {
            throw new InvalidDataException(
                $"Unknown presynaptic neuron root ID: " +
                $"{connection.PresynapticRootId}");
        }

        if (!neuronIndexMap.TryGetIndex(
                connection.PostsynapticRootId,
                out var postsynapticIndex))
        {
            throw new InvalidDataException(
                $"Unknown postsynaptic neuron root ID: " +
                $"{connection.PostsynapticRootId}");
        }

        return (
            presynapticIndex,
            postsynapticIndex);
    }
}