using FlyDoom.Neural.Simulation;

namespace FlyDoom.Neural.Models;

/// <summary>
/// Advances a population of neurons using a leaky integrate-and-fire model.
/// </summary>
public sealed class LifNeuronModel
{
    private readonly LifNeuronParameters _parameters;

    public LifNeuronModel(
        LifNeuronParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(
            parameters);

        _parameters =
            parameters;
    }

    /// <summary>
    /// Advances every neuron by one simulation timestep.
    /// </summary>
    /// <param name="state">
    /// Mutable neuron state to update.
    /// </param>
    /// <param name="timeStepMs">
    /// Duration of the simulation step in milliseconds.
    /// </param>
    public void Step(
        NeuronStateTable state,
        float timeStepMs)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        if (timeStepMs <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeStepMs));
        }

        var membranePotentials =
            state.MembranePotentialsMv;

        var synapticDrive =
            state.SynapticDriveMv;

        var refractoryRemaining =
            state.RefractoryRemainingMs;

        var fired =
            state.Fired;

        for (var neuronIndex = 0;
             neuronIndex < state.Count;
             neuronIndex++)
        {
            fired[neuronIndex] =
                false;

            if (refractoryRemaining[neuronIndex] > 0)
            {
                refractoryRemaining[neuronIndex] =
                    MathF.Max(
                        0,
                        refractoryRemaining[neuronIndex] -
                        timeStepMs);

                membranePotentials[neuronIndex] =
                    _parameters.ResetPotentialMv;

                synapticDrive[neuronIndex] =
                    0;

                continue;
            }

            var membranePotential =
                membranePotentials[neuronIndex];

            var drive =
                synapticDrive[neuronIndex];

            // Euler integration of:
            //
            // dV/dt =
            // (Vrest - V + synapticDrive) / membraneTimeConstant
            //
            // Synaptic drive is currently represented as an equivalent
            // voltage contribution rather than a detailed conductance model.
            membranePotential +=
                timeStepMs /
                _parameters.MembraneTimeConstantMs *
                (
                    _parameters.RestingPotentialMv -
                    membranePotential +
                    drive
                );

            // Input is consumed by this timestep. New synaptic events will
            // accumulate drive for a future step.
            synapticDrive[neuronIndex] =
                0;

            if (membranePotential >=
                _parameters.ThresholdPotentialMv)
            {
                fired[neuronIndex] =
                    true;

                membranePotential =
                    _parameters.ResetPotentialMv;

                refractoryRemaining[neuronIndex] =
                    _parameters.RefractoryPeriodMs;
            }

            membranePotentials[neuronIndex] =
                membranePotential;
        }
    }
}