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
    public void Step(
        NeuronStateTable state,
        float timeStepMs)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        if (timeStepMs <= 0 ||
            !float.IsFinite(timeStepMs))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeStepMs));
        }

        var membranePotentials =
            state.MembranePotentialsMv;

        var synapticInputs =
            state.SynapticInputsMv;

        var externalDrives =
            state.ExternalDrivesMv;

        var refractoryRemaining =
            state.RefractoryRemainingMs;

        var fired =
            state.Fired;

        //
        // Synaptic input follows exponential decay:
        //
        // S(t + dt) = S(t) * exp(-dt / tauSyn)
        //
        // A synaptic event therefore continues to influence the neuron
        // across several subsequent timesteps.
        //
        var synapticDecayFactor =
            MathF.Exp(
                -timeStepMs /
                _parameters.SynapticTimeConstantMs);

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

                //
                // Synaptic activity continues to decay while the neuron
                // is refractory rather than disappearing instantly.
                //
                synapticInputs[neuronIndex] *=
                    synapticDecayFactor;

                //
                // External stimulation belongs only to this timestep.
                //
                externalDrives[neuronIndex] =
                    0;

                continue;
            }

            var membranePotential =
                membranePotentials[
                    neuronIndex];

            var totalDrive =
                synapticInputs[neuronIndex] +
                externalDrives[neuronIndex];

            //
            // Current-based reference LIF model:
            //
            // dV/dt =
            // (Vrest - V + synapticDrive + externalDrive) / tauMembrane
            //
            // Drive values are voltage-equivalent terms inside this
            // equation. They are not direct membrane-voltage changes.
            //
            membranePotential +=
                timeStepMs /
                _parameters.MembraneTimeConstantMs *
                (
                    _parameters.RestingPotentialMv -
                    membranePotential +
                    totalDrive
                );

            //
            // External drive is a one-step stimulus.
            //
            externalDrives[neuronIndex] =
                0;

            //
            // Recurrent synaptic input persists and decays.
            //
            synapticInputs[neuronIndex] *=
                synapticDecayFactor;

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