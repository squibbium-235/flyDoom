namespace FlyDoom.Vision.Model;

/// <summary>
/// Stores conservative R1-R6 photoreceptor-to-cartridge assignments.
/// </summary>
public sealed class R1R6CartridgeMap
{
    private readonly R1R6CartridgeAssignment?[]
        _assignmentsByNeuron;

    private readonly Dictionary<string, int[]>
        _photoreceptorsByColumn;

    /// <summary>
    /// Gets the number of neurons represented by the underlying connectome.
    /// </summary>
    public int NeuronCount =>
        _assignmentsByNeuron.Length;

    /// <summary>
    /// Gets the number of R1-R6 neurons examined while building the map.
    /// </summary>
    public int CandidatePhotoreceptorCount { get; }

    /// <summary>
    /// Gets the number of R1-R6 neurons for which L1 and L2 independently
    /// agreed on the same cartridge.
    /// </summary>
    public int MappedPhotoreceptorCount { get; }

    /// <summary>
    /// Gets the number of R1-R6 neurons left unresolved.
    /// </summary>
    public int UnmappedPhotoreceptorCount =>
        CandidatePhotoreceptorCount -
        MappedPhotoreceptorCount;

    internal R1R6CartridgeMap(
        int neuronCount,
        int candidatePhotoreceptorCount,
        R1R6CartridgeAssignment?[] assignmentsByNeuron,
        Dictionary<string, int[]> photoreceptorsByColumn)
    {
        if (neuronCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(neuronCount));
        }

        ArgumentNullException.ThrowIfNull(
            assignmentsByNeuron);

        ArgumentNullException.ThrowIfNull(
            photoreceptorsByColumn);

        if (assignmentsByNeuron.Length !=
            neuronCount)
        {
            throw new ArgumentException(
                "Assignment table length must match neuron count.",
                nameof(assignmentsByNeuron));
        }

        CandidatePhotoreceptorCount =
            candidatePhotoreceptorCount;

        MappedPhotoreceptorCount =
            assignmentsByNeuron.Count(
                assignment =>
                    assignment.HasValue);

        _assignmentsByNeuron =
            assignmentsByNeuron;

        _photoreceptorsByColumn =
            photoreceptorsByColumn;
    }

    /// <summary>
    /// Attempts to get the inferred cartridge for one neuron.
    /// </summary>
    public bool TryGetAssignment(
        int neuronIndex,
        out R1R6CartridgeAssignment assignment)
    {
        ValidateNeuronIndex(
            neuronIndex);

        var stored =
            _assignmentsByNeuron[
                neuronIndex];

        if (stored is null)
        {
            assignment =
                default;

            return false;
        }

        assignment =
            stored.Value;

        return true;
    }

    /// <summary>
    /// Gets the R1-R6 photoreceptors inferred to represent one visual column.
    /// </summary>
    public IReadOnlyList<int> GetPhotoreceptorIndices(
        VisualColumn column)
    {
        ArgumentNullException.ThrowIfNull(
            column);

        var key =
            CreateColumnKey(
                column.Hemisphere,
                column.ColumnId);

        return _photoreceptorsByColumn.TryGetValue(
            key,
            out var photoreceptors)
            ? photoreceptors
            : Array.Empty<int>();
    }

    internal static string CreateColumnKey(
        string hemisphere,
        string columnId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            hemisphere);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            columnId);

        return
            hemisphere
                .Trim()
                .ToLowerInvariant() +
            "\u001F" +
            columnId.Trim();
    }

    private void ValidateNeuronIndex(
        int neuronIndex)
    {
        if ((uint)neuronIndex >=
            (uint)_assignmentsByNeuron.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(neuronIndex));
        }
    }
}