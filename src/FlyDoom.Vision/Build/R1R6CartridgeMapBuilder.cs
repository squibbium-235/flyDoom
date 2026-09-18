using FlyDoom.Connectome.Model;
using FlyDoom.Vision.Model;

namespace FlyDoom.Vision.Build;

/// <summary>
/// Infers the retinotopic lamina cartridge targeted by R1-R6 photoreceptors.
/// </summary>
public static class R1R6CartridgeMapBuilder
{
    /// <summary>
    /// Builds conservative R1-R6 cartridge assignments from real structural
    /// connectivity to column-assigned L1 and L2 neurons.
    /// </summary>
    /// <remarks>
    /// A photoreceptor is mapped only when:
    ///
    /// 1. it has usable lamina connectivity to both L1 and L2 neurons;
    /// 2. the uniquely strongest L1-supported column exists;
    /// 3. the uniquely strongest L2-supported column exists;
    /// 4. both independently identify the same column.
    ///
    /// Ambiguous or incomplete cases remain unmapped rather than being guessed.
    /// </remarks>
    public static R1R6CartridgeMap Build(
        VisualNeuronCatalog visualCatalog,
        CompactConnectome connectome)
    {
        ArgumentNullException.ThrowIfNull(
            visualCatalog);

        ArgumentNullException.ThrowIfNull(
            connectome);

        if (visualCatalog.Count !=
            connectome.NeuronCount)
        {
            throw new ArgumentException(
                "Visual catalogue and connectome must describe " +
                "the same neuron population.");
        }

        var assignments =
            new R1R6CartridgeAssignment?[
                connectome.NeuronCount];

        var photoreceptorsByColumn =
            new Dictionary<
                string,
                List<int>>(
                    StringComparer.Ordinal);

        var candidateCount =
            0;

        for (var presynapticIndex = 0;
             presynapticIndex < connectome.NeuronCount;
             presynapticIndex++)
        {
            if (!IsR1R6(
                    visualCatalog.GetType(
                        presynapticIndex)))
            {
                continue;
            }

            candidateCount++;

            var l1SynapsesByColumn =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);

            var l2SynapsesByColumn =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);

            var columnIdentities =
                new Dictionary<
                    string,
                    ColumnIdentity>(
                        StringComparer.Ordinal);

            var targets =
                connectome.GetPostsynapticIndices(
                    presynapticIndex);

            var synapseCounts =
                connectome.GetSynapseCounts(
                    presynapticIndex);

            var neuropilIndices =
                connectome.GetNeuropilIndices(
                    presynapticIndex);

            for (var connectionIndex = 0;
                 connectionIndex < targets.Length;
                 connectionIndex++)
            {
                var neuropil =
                    connectome.GetNeuropilName(
                        neuropilIndices[
                            connectionIndex]);

                //
                // R1-R6 form their principal output terminals in the lamina.
                // Connections outside LA_L / LA_R are not used as cartridge
                // landmarks.
                //

                if (!IsLaminaNeuropil(
                        neuropil))
                {
                    continue;
                }

                var postsynapticIndex =
                    targets[
                        connectionIndex];

                var postsynapticType =
                    visualCatalog.GetType(
                        postsynapticIndex);

                var isL1 =
                    string.Equals(
                        postsynapticType,
                        "L1",
                        StringComparison.OrdinalIgnoreCase);

                var isL2 =
                    string.Equals(
                        postsynapticType,
                        "L2",
                        StringComparison.OrdinalIgnoreCase);

                if (!isL1 &&
                    !isL2)
                {
                    continue;
                }

                if (!visualCatalog.HasColumnAssignment(
                        postsynapticIndex))
                {
                    continue;
                }

                var hemisphere =
                    visualCatalog.GetColumnHemisphere(
                        postsynapticIndex);

                var columnId =
                    visualCatalog.GetColumnId(
                        postsynapticIndex);

                if (string.IsNullOrWhiteSpace(
                        hemisphere) ||
                    string.IsNullOrWhiteSpace(
                        columnId))
                {
                    continue;
                }

                var key =
                    R1R6CartridgeMap.CreateColumnKey(
                        hemisphere,
                        columnId);

                columnIdentities[
                    key] =
                    new ColumnIdentity(
                        hemisphere.Trim(),
                        columnId.Trim());

                var synapseCount =
                    synapseCounts[
                        connectionIndex];

                if (isL1)
                {
                    AddSynapses(
                        l1SynapsesByColumn,
                        key,
                        synapseCount);
                }

                if (isL2)
                {
                    AddSynapses(
                        l2SynapsesByColumn,
                        key,
                        synapseCount);
                }
            }

            //
            // L1 and L2 must independently choose unique winning cartridges.
            // This avoids resolving an ambiguous neuron merely because one
            // combined score happened to be slightly larger.
            //

            if (!TryFindUniqueStrongest(
                    l1SynapsesByColumn,
                    out var strongestL1Column,
                    out var strongestL1Synapses))
            {
                continue;
            }

            if (!TryFindUniqueStrongest(
                    l2SynapsesByColumn,
                    out var strongestL2Column,
                    out var strongestL2Synapses))
            {
                continue;
            }

            if (!string.Equals(
                    strongestL1Column,
                    strongestL2Column,
                    StringComparison.Ordinal))
            {
                continue;
            }

            var identity =
                columnIdentities[
                    strongestL1Column];

            var totalLandmarkSynapses =
                l1SynapsesByColumn
                    .Values
                    .Sum() +
                l2SynapsesByColumn
                    .Values
                    .Sum();

            var assignment =
                new R1R6CartridgeAssignment(
                    presynapticIndex,
                    identity.Hemisphere,
                    identity.ColumnId,
                    strongestL1Synapses,
                    strongestL2Synapses,
                    totalLandmarkSynapses);

            assignments[
                presynapticIndex] =
                assignment;

            if (!photoreceptorsByColumn
                .TryGetValue(
                    strongestL1Column,
                    out var columnPhotoreceptors))
            {
                columnPhotoreceptors =
                    [];

                photoreceptorsByColumn.Add(
                    strongestL1Column,
                    columnPhotoreceptors);
            }

            columnPhotoreceptors.Add(
                presynapticIndex);
        }

        var compactColumnMap =
            photoreceptorsByColumn
                .ToDictionary(
                    pair =>
                        pair.Key,
                    pair =>
                        pair.Value.ToArray(),
                    StringComparer.Ordinal);

        return new R1R6CartridgeMap(
            connectome.NeuronCount,
            candidateCount,
            assignments,
            compactColumnMap);
    }

    private static bool IsR1R6(
        string? visualType)
    {
        return string.Equals(
            visualType,
            "R1-6",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLaminaNeuropil(
        string neuropil)
    {
        return string.Equals(
                   neuropil,
                   "LA_L",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   neuropil,
                   "LA_R",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static void AddSynapses(
        Dictionary<string, int> totals,
        string columnKey,
        int synapseCount)
    {
        if (synapseCount <= 0)
        {
            return;
        }

        if (totals.TryGetValue(
                columnKey,
                out var existing))
        {
            totals[columnKey] =
                checked(
                    existing +
                    synapseCount);
        }
        else
        {
            totals.Add(
                columnKey,
                synapseCount);
        }
    }

    private static bool TryFindUniqueStrongest(
        Dictionary<string, int> totals,
        out string strongestColumn,
        out int strongestSynapses)
    {
        strongestColumn =
            string.Empty;

        strongestSynapses =
            0;

        var tied =
            false;

        foreach (var pair in totals)
        {
            if (pair.Value >
                strongestSynapses)
            {
                strongestColumn =
                    pair.Key;

                strongestSynapses =
                    pair.Value;

                tied =
                    false;
            }
            else if (pair.Value ==
                     strongestSynapses)
            {
                tied =
                    true;
            }
        }

        return strongestSynapses > 0 &&
               !tied;
    }

    private readonly record struct ColumnIdentity(
        string Hemisphere,
        string ColumnId);
}