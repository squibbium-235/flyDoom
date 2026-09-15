using FlyDoom.Connectome.Model;
using FlyDoom.Neural.Models;
using FlyDoom.Neural.Transmission;

namespace FlyDoom.Neural.Simulation;

/// <summary>
/// Coordinates neural dynamics and propagation through a connectome.
/// </summary>
public sealed class NeuralSimulation
{
    private readonly CompactConnectome _connectome;
    private readonly NeuronStateTable _state;
    private readonly LifNeuronModel _neuronModel;
    private readonly ISynapticEffectModel _synapticEffectModel;

    /// <summary>
    /// Gets the mutable runtime state of the simulated neurons.
    /// </summary>
    public NeuronStateTable State => _state;

    /// <summary>
    /// Gets the amount of simulated time that has elapsed.
    /// </summary>
    public double SimulationTimeMs { get; private set; }

    public NeuralSimulation(
        CompactConnectome connectome,
        NeuronStateTable state,
        LifNeuronModel neuronModel,
        ISynapticEffectModel synapticEffectModel)
    {
        ArgumentNullException.ThrowIfNull(
            connectome);

        ArgumentNullException.ThrowIfNull(
            state);

        ArgumentNullException.ThrowIfNull(
            neuronModel);

        ArgumentNullException.ThrowIfNull(
            synapticEffectModel);

        if (connectome.NeuronCount !=
            state.Count)
        {
            throw new ArgumentException(
                "Connectome and neuron state must contain " +
                "the same number of neurons.",
                nameof(state));
        }

        _connectome =
            connectome;

        _state =
            state;

        _neuronModel =
            neuronModel;

        _synapticEffectModel =
            synapticEffectModel;
    }

    /// <summary>
    /// Advances the neural simulation by one timestep.
    /// </summary>
    /// <remarks>
    /// Neurons are first updated for the current timestep. Spikes produced
    /// during that update then generate synaptic drive that will be consumed
    /// during the following timestep. This gives propagation a minimum delay
    /// of one simulation step.
    /// </remarks>
    public void Step(
        float timeStepMs)
    {
        if (timeStepMs <= 0 ||
            !float.IsFinite(timeStepMs))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeStepMs));
        }

        _neuronModel.Step(
            _state,
            timeStepMs);

        PropagateSpikes();

        SimulationTimeMs +=
            timeStepMs;
    }

    /// <summary>
    /// Converts spikes from the current timestep into postsynaptic input.
    /// </summary>
    private void PropagateSpikes()
    {
        var fired =
            _state.Fired;

        for (var presynapticIndex = 0;
             presynapticIndex < _connectome.NeuronCount;
             presynapticIndex++)
        {
            if (!fired[presynapticIndex])
            {
                continue;
            }

            var targets =
                _connectome.GetPostsynapticIndices(
                    presynapticIndex);

            var synapseCounts =
                _connectome.GetSynapseCounts(
                    presynapticIndex);

            var neuropils =
                _connectome.GetNeuropilIndices(
                    presynapticIndex);

            var neurotransmitters =
                _connectome.GetNeurotransmitterTypes(
                    presynapticIndex);

            for (var connectionIndex = 0;
                 connectionIndex < targets.Length;
                 connectionIndex++)
            {
                var postsynapticIndex =
                    targets[connectionIndex];

                var drive =
                    _synapticEffectModel.CalculateDriveMv(
                        presynapticIndex,
                        postsynapticIndex,
                        synapseCounts[connectionIndex],
                        neuropils[connectionIndex],
                        neurotransmitters[connectionIndex]);

                _state.AddSynapticDriveMv(
                    postsynapticIndex,
                    drive);
            }
        }
    }
}