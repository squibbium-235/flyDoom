namespace FlyDoom.Neural.Simulation;

/// <summary>
/// Stores the mutable runtime state of every simulated neuron.
/// </summary>
/// <remarks>
/// State is stored in contiguous arrays rather than one object per neuron
/// so that large simulations can update neurons efficiently.
/// </remarks>
public sealed class NeuronStateTable
{
    private readonly float[] _membranePotentialsMv;
    private readonly float[] _synapticDriveMv;
    private readonly float[] _refractoryRemainingMs;
    private readonly bool[] _fired;

    /// <summary>
    /// Gets the number of neurons represented by the table.
    /// </summary>
    public int Count => _membranePotentialsMv.Length;

    /// <summary>
    /// Initialises runtime state for a population of neurons.
    /// </summary>
    /// <param name="neuronCount">
    /// Number of neurons in the population.
    /// </param>
    /// <param name="initialMembranePotentialMv">
    /// Initial membrane potential in millivolts.
    /// </param>
    public NeuronStateTable(
        int neuronCount,
        float initialMembranePotentialMv)
    {
        if (neuronCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(neuronCount));
        }

        _membranePotentialsMv =
            new float[neuronCount];

        _synapticDriveMv =
            new float[neuronCount];

        _refractoryRemainingMs =
            new float[neuronCount];

        _fired =
            new bool[neuronCount];

        Array.Fill(
            _membranePotentialsMv,
            initialMembranePotentialMv);
    }

    /// <summary>
    /// Gets a neuron's membrane potential in millivolts.
    /// </summary>
    public float GetMembranePotentialMv(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);

        return _membranePotentialsMv[
            neuronIndex];
    }

    /// <summary>
    /// Adds synaptic drive to a neuron for the next simulation step.
    /// </summary>
    /// <remarks>
    /// Synaptic drive is currently expressed as an equivalent voltage drive.
    /// Later synapse models can replace this with conductance-based input.
    /// </remarks>
    public void AddSynapticDriveMv(
        int neuronIndex,
        float driveMv)
    {
        ValidateIndex(neuronIndex);

        _synapticDriveMv[neuronIndex] +=
            driveMv;
    }

    /// <summary>
    /// Gets the synaptic drive currently accumulated by a neuron.
    /// </summary>
    public float GetSynapticDriveMv(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);

        return _synapticDriveMv[
            neuronIndex];
    }

    /// <summary>
    /// Gets the remaining refractory time for a neuron.
    /// </summary>
    public float GetRefractoryRemainingMs(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);

        return _refractoryRemainingMs[
            neuronIndex];
    }

    /// <summary>
    /// Gets whether a neuron fired during the most recent simulation step.
    /// </summary>
    public bool DidFire(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);

        return _fired[
            neuronIndex];
    }

    internal Span<float> MembranePotentialsMv =>
        _membranePotentialsMv;

    internal Span<float> SynapticDriveMv =>
        _synapticDriveMv;

    internal Span<float> RefractoryRemainingMs =>
        _refractoryRemainingMs;

    internal Span<bool> Fired =>
        _fired;

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