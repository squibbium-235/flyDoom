using FlyDoom.Connectome.Build;
using FlyDoom.Connectome.Import;
using FlyDoom.Connectome.Model;
using FlyDoom.Neural.Models;
using FlyDoom.Neural.Simulation;
using FlyDoom.Neural.Transmission;
using FlyDoom.Vision.Build;
using FlyDoom.Vision.Stimulation;

namespace FlyDoom.Runtime;

/// <summary>
/// Builds a complete FlyDoom runtime from a local FAFB dataset.
/// </summary>
public static class FlyDoomBootstrap
{
    /// <summary>
    /// Loads FAFB v783 and initialises the current reference neural and visual
    /// models.
    /// </summary>
    /// <param name="dataDirectory">
    /// Directory containing the raw FAFB v783 compressed CSV files.
    /// </param>
    /// <returns>
    /// A fully initialised runtime ready for simulation.
    /// </returns>
    public static FlyDoomRuntime LoadFafbV783(
        string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            dataDirectory);

        var dataset =
            new FafbDatasetReader(
                dataDirectory);

        //
        // The neuron list defines the compact index space shared by every
        // component of the simulation.
        //

        var neurons =
            dataset
                .ReadNeurons()
                .ToList();

        var neuronIndexMap =
            NeuronIndexMap.Create(
                neurons.Select(
                    neuron => neuron.RootId));

        //
        // Build static biological and annotation data.
        //

        var neuronTable =
            CompactNeuronTableBuilder.Build(
                neurons,
                neuronIndexMap);

        var identityTable =
            CompactNeuronIdentityTableBuilder.Build(
                neuronIndexMap,
                dataset.ReadNames(),
                dataset.ReadClassifications(),
                dataset.ReadCellTypes());

        var positionTable =
            CompactNeuronPositionTableBuilder.Build(
                () => dataset.ReadCoordinates(),
                neuronIndexMap);

        var morphologyTable =
            CompactNeuronMorphologyTableBuilder.Build(
                dataset.ReadCellStats(),
                neuronIndexMap);

        //
        // Build visual-system topology separately from the structural
        // connectome so the visual front end can address spatial columns
        // directly.
        //

        var visualCatalog =
            VisualNeuronCatalogBuilder.Build(
                neuronIndexMap,
                dataset.ReadVisualNeuronTypes(),
                dataset.ReadColumnAssignments());

        var visualColumns =
            VisualColumnMapBuilder.Build(
                visualCatalog);

        //
        // Build structural connectivity.
        //

        var connectome =
            CompactConnectomeBuilder.Build(
                neuronIndexMap,
                () => dataset.ReadConnections());

        var postsynapticInputs =
            PostsynapticInputTable.Build(
                connectome);

        //
        // Initialise mutable runtime neural state.
        //
        // These parameters remain provisional reference-model assumptions.
        // They should not be interpreted as universal measured values for
        // every neuron in the fly.
        //

        var neuralParameters =
            LifNeuronParameters.Default;

        var neuralState =
            new NeuronStateTable(
                connectome.NeuronCount,
                neuralParameters.RestingPotentialMv);

        var neuralSimulation =
            new NeuralSimulation(
                connectome,
                neuralState,
                new LifNeuronModel(
                    neuralParameters),
                new FastTransmitterSynapticEffectModel(
                    postsynapticInputs,
                    fullInputAmplitudeMv: 40f));

        //
        // R7/R8 visual stimulation currently uses graded histaminergic
        // transmission rather than forcing non-spiking photoreceptors through
        // the reference LIF model.
        //

        var photoreceptorStimulator =
            new ColumnPhotoreceptorStimulator(
                visualCatalog,
                connectome,
                postsynapticInputs,
                fullHistamineInputAmplitudeMv: 40f);

        return new FlyDoomRuntime(
            neuronIndexMap,
            neuronTable,
            identityTable,
            positionTable,
            morphologyTable,
            connectome,
            visualCatalog,
            visualColumns,
            neuralState,
            neuralSimulation,
            photoreceptorStimulator);
    }
}