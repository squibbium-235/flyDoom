namespace FlyDoom.Neural.Models;

/// <summary>
/// Defines parameters for a leaky integrate-and-fire neuron model.
/// </summary>
/// <remarks>
/// These defaults provide a starting simulation model and are not claimed
/// to be universal measured parameters for every Drosophila neuron.
/// Cell-type-specific parameters can replace them as biological evidence
/// is incorporated.
/// </remarks>
public sealed class LifNeuronParameters
{
    public float RestingPotentialMv { get; }

    public float ResetPotentialMv { get; }

    public float ThresholdPotentialMv { get; }

    public float MembraneTimeConstantMs { get; }

    public float RefractoryPeriodMs { get; }

    public LifNeuronParameters(
        float restingPotentialMv,
        float resetPotentialMv,
        float thresholdPotentialMv,
        float membraneTimeConstantMs,
        float refractoryPeriodMs)
    {
        if (membraneTimeConstantMs <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(membraneTimeConstantMs));
        }

        if (refractoryPeriodMs < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(refractoryPeriodMs));
        }

        if (thresholdPotentialMv <=
            resetPotentialMv)
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

        RefractoryPeriodMs =
            refractoryPeriodMs;
    }

    /// <summary>
    /// Gets provisional parameters suitable for initial simulator testing.
    /// </summary>
    public static LifNeuronParameters Default { get; } =
        new(
            restingPotentialMv: -60f,
            resetPotentialMv: -65f,
            thresholdPotentialMv: -45f,
            membraneTimeConstantMs: 20f,
            refractoryPeriodMs: 2f);
}