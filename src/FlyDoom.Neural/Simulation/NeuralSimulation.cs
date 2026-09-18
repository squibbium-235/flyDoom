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
    public NeuronStateTable State =>
        _state;

    /// <summary>
    /// Gets the amount of simulated time that has elapsed.
    /// </summary>
    public double SimulationTimeMs
    {
        get;
        private set;
    }

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
    /// Restores the short-term electrical simulation to its initial state.
    /// </summary>
    /// <remarks>
    /// Structural connectivity and future learned long-term parameters remain
    /// untouched. Only transient electrical state and simulation time reset.
    /// </remarks>
    public void Reset()
    {
        _state.Reset();

        SimulationTimeMs =
            0;
    }

    /// <summary>
    /// Advances the complete neural simulation by one timestep.
    /// </summary>
    public NeuralStepStatistics Step(
        float timeStepMs)
    {
        if (timeStepMs <= 0 ||
            !float.IsFinite(
                timeStepMs))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeStepMs));
        }

        _neuronModel.Step(
            _state,
            timeStepMs);

        var firedNeuronCount =
            CountFiredNeurons();

        var propagatedConnectionCount =
            PropagateSpikes();

        SimulationTimeMs +=
            timeStepMs;

        var activity =
            MeasureActivity();

        return new NeuralStepStatistics(
            SimulationTimeMs,
            firedNeuronCount,
            activity.ActiveSynapticNeuronCount,
            propagatedConnectionCount,
            activity.MaximumAbsoluteSynapticInputMv,
            activity.MinimumMembranePotentialMv,
            activity.MaximumMembranePotentialMv);
    }

    private int CountFiredNeurons()
    {
        var fired =
            _state.Fired;

        var count =
            0;

        for (var neuronIndex = 0;
             neuronIndex < fired.Length;
             neuronIndex++)
        {
            if (fired[
                    neuronIndex])
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Propagates spike-triggered synaptic output.
    /// </summary>
    /// <remarks>
    /// Graded sensory transmission, such as photoreceptor output, enters
    /// synaptic state separately and therefore does not need to generate
    /// action potentials in neurons that are biologically non-spiking.
    /// </remarks>
    private long PropagateSpikes()
    {
        var fired =
            _state.Fired;

        long propagatedConnectionCount =
            0;

        for (var presynapticIndex = 0;
             presynapticIndex <
             _connectome.NeuronCount;
             presynapticIndex++)
        {
            if (!fired[
                    presynapticIndex])
            {
                continue;
            }

            var targets =
                _connectome
                    .GetPostsynapticIndices(
                        presynapticIndex);

            var synapseCounts =
                _connectome
                    .GetSynapseCounts(
                        presynapticIndex);

            var neuropils =
                _connectome
                    .GetNeuropilIndices(
                        presynapticIndex);

            var neurotransmitters =
                _connectome
                    .GetNeurotransmitterTypes(
                        presynapticIndex);

            for (var connectionIndex = 0;
                 connectionIndex <
                 targets.Length;
                 connectionIndex++)
            {
                var postsynapticIndex =
                    targets[
                        connectionIndex];

                var inputAmplitude =
                    _synapticEffectModel
                        .CalculateInputAmplitudeMv(
                            presynapticIndex,
                            postsynapticIndex,
                            synapseCounts[
                                connectionIndex],
                            neuropils[
                                connectionIndex],
                            neurotransmitters[
                                connectionIndex]);

                if (inputAmplitude ==
                    0f)
                {
                    continue;
                }

                _state.AddSynapticInputMv(
                    postsynapticIndex,
                    inputAmplitude);

                propagatedConnectionCount++;
            }
        }

        return propagatedConnectionCount;
    }

    private (
        int ActiveSynapticNeuronCount,
        float MaximumAbsoluteSynapticInputMv,
        float MinimumMembranePotentialMv,
        float MaximumMembranePotentialMv)
        MeasureActivity()
    {
        var synapticInputs =
            _state.SynapticInputsMv;

        var membranePotentials =
            _state.MembranePotentialsMv;

        var activeSynapticNeuronCount =
            0;

        var maximumAbsoluteSynapticInputMv =
            0f;

        var minimumMembranePotentialMv =
            float.PositiveInfinity;

        var maximumMembranePotentialMv =
            float.NegativeInfinity;

        for (var neuronIndex = 0;
             neuronIndex < _state.Count;
             neuronIndex++)
        {
            var absoluteInput =
                MathF.Abs(
                    synapticInputs[
                        neuronIndex]);

            //
            // Ignore tiny floating-point remnants left after exponential
            // synaptic decay.
            //

            if (absoluteInput >
                0.0001f)
            {
                activeSynapticNeuronCount++;
            }

            if (absoluteInput >
                maximumAbsoluteSynapticInputMv)
            {
                maximumAbsoluteSynapticInputMv =
                    absoluteInput;
            }

            if (membranePotentials[
                    neuronIndex] <
                minimumMembranePotentialMv)
            {
                minimumMembranePotentialMv =
                    membranePotentials[
                        neuronIndex];
            }

            if (membranePotentials[
                    neuronIndex] >
                maximumMembranePotentialMv)
            {
                maximumMembranePotentialMv =
                    membranePotentials[
                        neuronIndex];
            }
        }

        return (
            activeSynapticNeuronCount,
            maximumAbsoluteSynapticInputMv,
            minimumMembranePotentialMv,
            maximumMembranePotentialMv);
    }
}