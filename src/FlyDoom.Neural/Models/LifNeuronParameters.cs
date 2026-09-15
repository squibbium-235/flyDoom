namespace FlyDoom.Neural.Models;

/// <summary>
/// Defines parameters for a leaky integrate-and-fire neuron model.
/// </summary>
/// <remarks>
/// These parameters form a provisional reference model. They are not
/// claimed to represent universal measured values for every Drosophila
/// neuron. More specific parameters can later be assigned from biological
/// evidence where available.
/// </remarks>
public sealed class LifNeuronParameters
{
    public float RestingPotentialMv { get; }

    public float ResetPotentialMv { get; }

    public float ThresholdPotentialMv { get; }

    public float MembraneTimeConstantMs { get; }

    public float SynapticTimeConstantMs { get; }

    public float RefractoryPeriodMs { get; }

    public LifNeuronParameters(
        float restingPotentialMv,
        float resetPotentialMv,
        float thresholdPotentialMv,
        float membraneTimeConstantMs,
        float synapticTimeConstantMs,
        float refractoryPeriodMs)
    {
        if (membraneTimeConstantMs <= 0 ||
            !float.IsFinite(membraneTimeConstantMs))
        {
            throw new ArgumentOutOfRangeException(
                nameof(membraneTimeConstantMs));
        }

        if (synapticTimeConstantMs <= 0 ||
            !float.IsFinite(synapticTimeConstantMs))
        {
            throw new ArgumentOutOfRangeException(
                nameof(synapticTimeConstantMs));
        }

        if (refractoryPeriodMs < 0 ||
            !float.IsFinite(refractoryPeriodMs))
        {
            throw new ArgumentOutOfRangeException(
                nameof(refractoryPeriodMs));
        }

        if (thresholdPotentialMv <= resetPotentialMv)
        {
            throw new ArgumentException(
                "Threshold potential must be greater than reset potential.",
                nameof(thresholdPotentialMv));
        }

        RestingPotentialMv =
            restingPotentialMv;

        ResetPotentialMv =
            resetPotentialMv;

        ThresholdPotentialMv =
            thresholdPotentialMv;

        MembraneTimeConstantMs =
            membraneTimeConstantMs;

        SynapticTimeConstantMs =
            synapticTimeConstantMs;

        RefractoryPeriodMs =
            refractoryPeriodMs;
    }

    /// <summary>
    /// Gets provisional parameters used by the reference simulator.
    /// </summary>
    public static LifNeuronParameters Default { get; } =
        new(
            restingPotentialMv: -60f,
            resetPotentialMv: -65f,
            thresholdPotentialMv: -45f,
            membraneTimeConstantMs: 20f,
            synapticTimeConstantMs: 5f,
            refractoryPeriodMs: 2f);
}