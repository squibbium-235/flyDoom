using FlyDoom.Connectome.Build;
using FlyDoom.Connectome.Import;
using FlyDoom.Connectome.Model;
using FlyDoom.Neural.Models;
using FlyDoom.Neural.Simulation;
using FlyDoom.Neural.Transmission;
using FlyDoom.Vision.Build;
using FlyDoom.Vision.Stimulation;
using FlyDoom.Vision.Transmission;

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
    public static FlyDoomRuntime LoadFafbV783(
        string dataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            dataDirectory);

        var dataset =
            new FafbDatasetReader(
                dataDirectory);

        //
        // The master neuron table defines the compact integer index space used
        // throughout the simulation.
        //

        var neurons =
            dataset
                .ReadNeurons()
                .ToList();

        var neuronIndexMap =
            NeuronIndexMap.Create(
                neurons.Select(
                    neuron =>
                        neuron.RootId));

        //
        // Static biological metadata.
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
                () =>
                    dataset.ReadCoordinates(),
                neuronIndexMap);

        var morphologyTable =
            CompactNeuronMorphologyTableBuilder.Build(
                dataset.ReadCellStats(),
                neuronIndexMap);

        //
        // Visual-system annotation.
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
        // Structural FAFB connectome.
        //

        var connectome =
            CompactConnectomeBuilder.Build(
                neuronIndexMap,
                () =>
                    dataset.ReadConnections());

        //
        // Reconstruct the R1-R6 neural-superposition cartridges from actual
        // FAFB connectivity to column-assigned L1/L2 landmarks.
        //

        var r1R6CartridgeMap =
            R1R6CartridgeMapBuilder.Build(
                visualCatalog,
                connectome);

        //
        // Total structural input is used as a normalisation reference.
        //
        // Synapse count remains anatomical evidence rather than being treated
        // directly as an electrical weight.
        //

        var postsynapticInputs =
            PostsynapticInputTable.Build(
                connectome);

        //
        // Mutable neural state.
        //
        // LIF remains a provisional reference model for neurons without a
        // better evidence-backed electrophysiological representation.
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
        // Graded transmission for L1/L2/L3.
        //
        // referenceVoltageRangeMv:
        //     Membrane deviation producing saturated graded output.
        //
        // fullEffectAmplitudeMv:
        //     Same provisional full-input scale used by the current fast
        //     synaptic reference model.
        //
        // synapticTimeConstantMs:
        //     Matches the current 5 ms synaptic decay used elsewhere in the
        //     reference simulation.
        //

        var gradedVisualTransmission =
            new GradedVisualTransmissionModel(
                visualCatalog,
                connectome,
                postsynapticInputs,
                restingPotentialMv:
                    neuralParameters.RestingPotentialMv,
                referenceVoltageRangeMv:
                    20f,
                fullEffectAmplitudeMv:
                    40f,
                synapticTimeConstantMs:
                    5f);

        //
        // Photoreceptors themselves are graded sensory neurons.
        //
        // They currently enter the simulation through an explicit histaminergic
        // stimulus model rather than the generic LIF spike path.
        //

        var photoreceptorStimulator =
            new ColumnPhotoreceptorStimulator(
                visualCatalog,
                connectome,
                postsynapticInputs,
                r1R6CartridgeMap,
                fullHistamineInputAmplitudeMv:
                    40f);

        return new FlyDoomRuntime(
            neuronIndexMap,
            neuronTable,
            identityTable,
            positionTable,
            morphologyTable,
            connectome,
            visualCatalog,
            visualColumns,
            r1R6CartridgeMap,
            neuralState,
            neuralSimulation,
            gradedVisualTransmission,
            photoreceptorStimulator);
    }
}