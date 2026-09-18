using FlyDoom.Connectome.Model;
using FlyDoom.Neural.Simulation;
using FlyDoom.Vision.Model;
using FlyDoom.Vision.Stimulation;
using FlyDoom.Vision.Transmission;

namespace FlyDoom.Runtime;

/// <summary>
/// Contains the loaded biological data and mutable simulation state for one
/// FlyDoom runtime instance.
/// </summary>
/// <remarks>
/// This class is deliberately UI-independent. Console applications, graphical
/// front ends, training systems, and future DOOM integration should operate
/// on this shared runtime rather than assembling independent copies of the
/// simulated fly.
/// </remarks>
public sealed class FlyDoomRuntime
{
    public NeuronIndexMap NeuronIndexMap { get; }

    public CompactNeuronTable NeuronTable { get; }

    public CompactNeuronIdentityTable IdentityTable { get; }

    public CompactNeuronPositionTable PositionTable { get; }

    public CompactNeuronMorphologyTable MorphologyTable { get; }

    public CompactConnectome Connectome { get; }

    /// <summary>
    /// Gets visual-system annotations aligned with compact simulation indices.
    /// </summary>
    public VisualNeuronCatalog VisualCatalog { get; }

    public VisualColumnMap VisualColumns { get; }

    public R1R6CartridgeMap R1R6CartridgeMap { get; }

    public NeuronStateTable NeuralState { get; }

    public NeuralSimulation NeuralSimulation { get; }

    public GradedVisualTransmissionModel
        GradedVisualTransmission
    {
        get;
    }

    public ColumnPhotoreceptorStimulator
        PhotoreceptorStimulator
    {
        get;
    }

    /// <summary>
    /// Gets statistics from the most recent graded-transmission pass.
    /// </summary>
    public GradedTransmissionStatistics
        LastGradedTransmission
    {
        get;
        private set;
    }

    internal FlyDoomRuntime(
        NeuronIndexMap neuronIndexMap,
        CompactNeuronTable neuronTable,
        CompactNeuronIdentityTable identityTable,
        CompactNeuronPositionTable positionTable,
        CompactNeuronMorphologyTable morphologyTable,
        CompactConnectome connectome,
        VisualNeuronCatalog visualCatalog,
        VisualColumnMap visualColumns,
        R1R6CartridgeMap r1R6CartridgeMap,
        NeuronStateTable neuralState,
        NeuralSimulation neuralSimulation,
        GradedVisualTransmissionModel gradedVisualTransmission,
        ColumnPhotoreceptorStimulator photoreceptorStimulator)
    {
        NeuronIndexMap =
            neuronIndexMap;

        NeuronTable =
            neuronTable;

        IdentityTable =
            identityTable;

        PositionTable =
            positionTable;

        MorphologyTable =
            morphologyTable;

        Connectome =
            connectome;

        VisualCatalog =
            visualCatalog;

        VisualColumns =
            visualColumns;

        R1R6CartridgeMap =
            r1R6CartridgeMap;

        NeuralState =
            neuralState;

        NeuralSimulation =
            neuralSimulation;

        GradedVisualTransmission =
            gradedVisualTransmission;

        PhotoreceptorStimulator =
            photoreceptorStimulator;
    }

    /// <summary>
    /// Resets transient electrical state while preserving the loaded brain and
    /// any future long-term learned state.
    /// </summary>
    /// <remarks>
    /// This is intended for reproducible experiments. Two visual sweeps can
    /// begin from the exact same neural state without rebuilding the connectome
    /// or discarding future plasticity.
    /// </remarks>
    public void ResetDynamicState()
    {
        NeuralSimulation.Reset();

        LastGradedTransmission =
            default;
    }

    /// <summary>
    /// Advances the complete hybrid neural simulation.
    /// </summary>
    public NeuralStepStatistics Step(
        float timestepMs)
    {
        var neuralStatistics =
            NeuralSimulation.Step(
                timestepMs);

        LastGradedTransmission =
            GradedVisualTransmission.Propagate(
                NeuralState,
                timestepMs);

        return neuralStatistics;
    }
}