using FlyDoom.Core.Biology;

namespace FlyDoom.Connectome.Model;

/// <summary>
/// Stores per-neuron biological metadata in arrays aligned with compact
/// simulation indices.
/// </summary>
public sealed class CompactNeuronTable
{
    private readonly NeurotransmitterType[] _neurotransmitterTypes;
    private readonly float[] _neurotransmitterConfidence;
    private readonly float[] _dopamineScores;
    private readonly float[] _serotoninScores;
    private readonly float[] _gabaScores;
    private readonly float[] _glutamateScores;
    private readonly float[] _acetylcholineScores;
    private readonly float[] _octopamineScores;

    /// <summary>
    /// Gets the number of neurons represented by the table.
    /// </summary>
    public int Count => _neurotransmitterTypes.Length;

    public CompactNeuronTable(
        NeurotransmitterType[] neurotransmitterTypes,
        float[] neurotransmitterConfidence,
        float[] dopamineScores,
        float[] serotoninScores,
        float[] gabaScores,
        float[] glutamateScores,
        float[] acetylcholineScores,
        float[] octopamineScores)
    {
        ArgumentNullException.ThrowIfNull(neurotransmitterTypes);
        ArgumentNullException.ThrowIfNull(neurotransmitterConfidence);
        ArgumentNullException.ThrowIfNull(dopamineScores);
        ArgumentNullException.ThrowIfNull(serotoninScores);
        ArgumentNullException.ThrowIfNull(gabaScores);
        ArgumentNullException.ThrowIfNull(glutamateScores);
        ArgumentNullException.ThrowIfNull(acetylcholineScores);
        ArgumentNullException.ThrowIfNull(octopamineScores);

        var count = neurotransmitterTypes.Length;

        if (neurotransmitterConfidence.Length != count ||
            dopamineScores.Length != count ||
            serotoninScores.Length != count ||
            gabaScores.Length != count ||
            glutamateScores.Length != count ||
            acetylcholineScores.Length != count ||
            octopamineScores.Length != count)
        {
            throw new ArgumentException(
                "All neuron metadata arrays must have matching lengths.");
        }

        _neurotransmitterTypes = neurotransmitterTypes;
        _neurotransmitterConfidence = neurotransmitterConfidence;
        _dopamineScores = dopamineScores;
        _serotoninScores = serotoninScores;
        _gabaScores = gabaScores;
        _glutamateScores = glutamateScores;
        _acetylcholineScores = acetylcholineScores;
        _octopamineScores = octopamineScores;
    }

    public NeurotransmitterType GetNeurotransmitterType(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _neurotransmitterTypes[neuronIndex];
    }

    public float GetNeurotransmitterConfidence(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _neurotransmitterConfidence[neuronIndex];
    }

    public float GetDopamineScore(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _dopamineScores[neuronIndex];
    }

    public float GetSerotoninScore(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _serotoninScores[neuronIndex];
    }

    public float GetGabaScore(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _gabaScores[neuronIndex];
    }

    public float GetGlutamateScore(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _glutamateScores[neuronIndex];
    }

    public float GetAcetylcholineScore(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _acetylcholineScores[neuronIndex];
    }

    public float GetOctopamineScore(int neuronIndex)
    {
        ValidateNeuronIndex(neuronIndex);
        return _octopamineScores[neuronIndex];
    }

    private void ValidateNeuronIndex(int neuronIndex)
    {
        if ((uint)neuronIndex >= (uint)Count)
        {
            throw new ArgumentOutOfRangeException(nameof(neuronIndex));
        }
    }
}