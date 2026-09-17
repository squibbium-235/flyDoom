using FlyDoom.Connectome.Model;
using FlyDoom.Neural.Simulation;
using FlyDoom.Vision.Model;
using FlyDoom.Vision.Stimulation;

namespace FlyDoom.Runtime;

/// <summary>
/// Contains the loaded biological data and mutable simulation state for one
/// FlyDoom runtime instance.
/// </summary>
/// <remarks>
/// This class is deliberately UI-independent. Console applications, graphical
/// front ends, training systems, and future DOOM integration should all operate
/// on the same runtime model rather than assembling their own copies of the
/// fly.
/// </remarks>
public sealed class FlyDoomRuntime
{
    /// <summary>
    /// Gets the mapping between FlyWire root IDs and compact simulation indices.
    /// </summary>
    public NeuronIndexMap NeuronIndexMap { get; }

    /// <summary>
    /// Gets compact biological metadata for every neuron.
    /// </summary>
    public CompactNeuronTable NeuronTable { get; }

    /// <summary>
    /// Gets names and classification metadata for every neuron.
    /// </summary>
    public CompactNeuronIdentityTable IdentityTable { get; }

    /// <summary>
    /// Gets representative spatial positions for every neuron.
    /// </summary>
    public CompactNeuronPositionTable PositionTable { get; }

    /// <summary>
    /// Gets morphological measurements for neurons where available.
    /// </summary>
    public CompactNeuronMorphologyTable MorphologyTable { get; }

    /// <summary>
    /// Gets the compact FAFB structural connectome.
    /// </summary>
    public CompactConnectome Connectome { get; }

    /// <summary>
    /// Gets visual-system annotations aligned with compact simulation indices.
    /// </summary>
    public VisualNeuronCatalog VisualCatalog { get; }

    /// <summary>
    /// Gets the spatial visual-column map for both optic lobes.
    /// </summary>
    public VisualColumnMap VisualColumns { get; }

    /// <summary>
    /// Gets mutable neural state for the current simulation.
    /// </summary>
    public NeuronStateTable NeuralState { get; }

    /// <summary>
    /// Gets the whole-brain neural simulator.
    /// </summary>
    public NeuralSimulation NeuralSimulation { get; }

    /// <summary>
    /// Gets the current graded photoreceptor stimulus model.
    /// </summary>
    public ColumnPhotoreceptorStimulator PhotoreceptorStimulator { get; }

    internal FlyDoomRuntime(
        NeuronIndexMap neuronIndexMap,
        CompactNeuronTable neuronTable,
        CompactNeuronIdentityTable identityTable,
        CompactNeuronPositionTable positionTable,
        CompactNeuronMorphologyTable morphologyTable,
        CompactConnectome connectome,
        VisualNeuronCatalog visualCatalog,
        VisualColumnMap visualColumns,
        NeuronStateTable neuralState,
        NeuralSimulation neuralSimulation,
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

        NeuralState =
            neuralState;

        NeuralSimulation =
            neuralSimulation;

        PhotoreceptorStimulator =
            photoreceptorStimulator;
    }
}