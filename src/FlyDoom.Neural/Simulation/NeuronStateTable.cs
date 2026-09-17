namespace FlyDoom.Neural.Simulation;

/// <summary>
/// Stores mutable runtime state for every simulated neuron.
/// </summary>
/// <remarks>
/// State is stored in contiguous arrays rather than one object per neuron
/// so that whole-brain updates can be performed efficiently.
/// </remarks>
public sealed class NeuronStateTable
{
    private readonly float[] _membranePotentialsMv;
    private readonly float[] _synapticInputsMv;
    private readonly float[] _externalDrivesMv;
    private readonly float[] _refractoryRemainingMs;
    private readonly bool[] _fired;

    /// <summary>
    /// Gets the number of neurons represented by the table.
    /// </summary>
    public int Count => _membranePotentialsMv.Length;

    /// <summary>
    /// Initialises runtime state for a population of neurons.
    /// </summary>
    public NeuronStateTable(
        int neuronCount,
        float initialMembranePotentialMv)
    {
        if (neuronCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(neuronCount));
        }

        if (!float.IsFinite(initialMembranePotentialMv))
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialMembranePotentialMv));
        }

        _membranePotentialsMv =
            new float[neuronCount];

        _synapticInputsMv =
            new float[neuronCount];

        _externalDrivesMv =
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
    /// Gets a neuron's membrane potential.
    /// </summary>
    public float GetMembranePotentialMv(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);

        return _membranePotentialsMv[
            neuronIndex];
    }

    /// <summary>
    /// Gets the neuron's current decaying synaptic input.
    /// </summary>
    /// <remarks>
    /// This value is an equivalent voltage-drive term used by the
    /// reference LIF equation. It is not a direct change in membrane
    /// potential.
    /// </remarks>
    public float GetSynapticInputMv(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);

        return _synapticInputsMv[
            neuronIndex];
    }

    /// <summary>
    /// Adds persistent synaptic input produced by another neuron's spike.
    /// </summary>
    public void AddSynapticInputMv(
        int neuronIndex,
        float inputMv)
    {
        ValidateIndex(neuronIndex);

        if (!float.IsFinite(inputMv))
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputMv));
        }

        _synapticInputsMv[neuronIndex] +=
            inputMv;
    }

    /// <summary>
    /// Gets one-step external drive awaiting consumption by the neuron.
    /// </summary>
    public float GetExternalDriveMv(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);

        return _externalDrivesMv[
            neuronIndex];
    }

    /// <summary>
    /// Adds external drive that will be consumed during the next timestep.
    /// </summary>
    /// <remarks>
    /// External drive is separate from recurrent synaptic input. It provides
    /// a one-step injected stimulus for experiments. Graded sensory systems
    /// such as vision currently add decaying synaptic input instead.
    /// </remarks>
    public void AddExternalDriveMv(
        int neuronIndex,
        float driveMv)
    {
        ValidateIndex(neuronIndex);

        if (!float.IsFinite(driveMv))
        {
            throw new ArgumentOutOfRangeException(
                nameof(driveMv));
        }

        _externalDrivesMv[neuronIndex] +=
            driveMv;
    }

    /// <summary>
    /// Gets the remaining refractory time.
    /// </summary>
    public float GetRefractoryRemainingMs(
        int neuronIndex)
    {
        ValidateIndex(neuronIndex);

        return _refractoryRemainingMs[
            neuronIndex];
    }

    /// <summary>
    /// Gets whether the neuron fired during the most recent timestep.
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

    internal Span<float> SynapticInputsMv =>
        _synapticInputsMv;

    internal Span<float> ExternalDrivesMv =>
        _externalDrivesMv;

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