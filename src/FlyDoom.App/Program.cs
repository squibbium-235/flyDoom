using System.Diagnostics;
using FlyDoom.Connectome.Build;
using FlyDoom.Connectome.Import;
using FlyDoom.Connectome.Model;
using FlyDoom.Core.Biology;
using FlyDoom.Neural.Models;
using FlyDoom.Neural.Simulation;
using FlyDoom.Neural.Transmission;

var repoRoot =
    FindRepositoryRoot();

var dataDirectory =
    Path.Combine(
        repoRoot.FullName,
        "data",
        "fafb-v783",
        "raw");

Console.WriteLine(
    $"Repository: {repoRoot.FullName}");

Console.WriteLine(
    $"Data:       {dataDirectory}");

Console.WriteLine();
Console.WriteLine("FlyDoom FAFB v783");
Console.WriteLine("=================");
Console.WriteLine();

var dataset =
    new FafbDatasetReader(
        dataDirectory);

//
// Neuron identities and basic metadata
//

Console.WriteLine("Loading neurons...");

var neurons =
    dataset
        .ReadNeurons()
        .ToList();

Console.WriteLine(
    $"Neurons: {neurons.Count:N0}");

var neuronIndexMap =
    NeuronIndexMap.Create(
        neurons.Select(
            neuron => neuron.RootId));

Console.WriteLine(
    $"Neuron indices: 0 - " +
    $"{neuronIndexMap.Count - 1:N0}");

Console.WriteLine();
Console.WriteLine(
    "Building neuron metadata...");

var neuronTable =
    CompactNeuronTableBuilder.Build(
        neurons,
        neuronIndexMap);

Console.WriteLine(
    $"Neuron metadata: " +
    $"{neuronTable.Count:N0}");

//
// Identity metadata
//

Console.WriteLine();
Console.WriteLine(
    "Building neuron identity metadata...");

var identityTable =
    CompactNeuronIdentityTableBuilder.Build(
        neuronIndexMap,
        dataset.ReadNames(),
        dataset.ReadClassifications(),
        dataset.ReadCellTypes());

Console.WriteLine(
    $"Neuron identities: " +
    $"{identityTable.Count:N0}");

var namedNeurons = 0;
var typedNeurons = 0;
var classifiedNeurons = 0;
var sidedNeurons = 0;
var hemilineageNeurons = 0;

for (var neuronIndex = 0;
     neuronIndex < identityTable.Count;
     neuronIndex++)
{
    if (identityTable.GetName(
            neuronIndex) is not null)
    {
        namedNeurons++;
    }

    if (identityTable.GetPrimaryType(
            neuronIndex) is not null)
    {
        typedNeurons++;
    }

    if (identityTable.GetClass(
            neuronIndex) is not null)
    {
        classifiedNeurons++;
    }

    if (identityTable.GetSide(
            neuronIndex) is not null)
    {
        sidedNeurons++;
    }

    if (identityTable.GetHemilineage(
            neuronIndex) is not null)
    {
        hemilineageNeurons++;
    }
}

Console.WriteLine();
Console.WriteLine(
    "Identity metadata coverage:");

Console.WriteLine(
    $"  Named:        " +
    $"{namedNeurons,8:N0} " +
    $"({namedNeurons * 100.0 / identityTable.Count,5:F1}%)");

Console.WriteLine(
    $"  Primary type: " +
    $"{typedNeurons,8:N0} " +
    $"({typedNeurons * 100.0 / identityTable.Count,5:F1}%)");

Console.WriteLine(
    $"  Classified:   " +
    $"{classifiedNeurons,8:N0} " +
    $"({classifiedNeurons * 100.0 / identityTable.Count,5:F1}%)");

Console.WriteLine(
    $"  Side:         " +
    $"{sidedNeurons,8:N0} " +
    $"({sidedNeurons * 100.0 / identityTable.Count,5:F1}%)");

Console.WriteLine(
    $"  Hemilineage:  " +
    $"{hemilineageNeurons,8:N0} " +
    $"({hemilineageNeurons * 100.0 / identityTable.Count,5:F1}%)");

//
// Coordinate metadata
//

Console.WriteLine();
Console.WriteLine(
    "Building neuron positions...");

var positionTable =
    CompactNeuronPositionTableBuilder.Build(
        () => dataset.ReadCoordinates(),
        neuronIndexMap);

var positionedNeurons = 0;
var neuronsWithMultiplePositions = 0;

for (var neuronIndex = 0;
     neuronIndex < positionTable.NeuronCount;
     neuronIndex++)
{
    var positionCount =
        positionTable.GetPositionCount(
            neuronIndex);

    if (positionCount > 0)
    {
        positionedNeurons++;
    }

    if (positionCount > 1)
    {
        neuronsWithMultiplePositions++;
    }
}

Console.WriteLine(
    $"Coordinate records: " +
    $"{positionTable.PositionCount:N0}");

Console.WriteLine(
    $"Neurons with coordinates: " +
    $"{positionedNeurons:N0} / " +
    $"{positionTable.NeuronCount:N0}");

Console.WriteLine(
    $"Neurons with multiple coordinates: " +
    $"{neuronsWithMultiplePositions:N0}");

//
// Morphological metadata
//

Console.WriteLine();
Console.WriteLine(
    "Building neuron morphology metadata...");

var morphologyTable =
    CompactNeuronMorphologyTableBuilder.Build(
        dataset.ReadCellStats(),
        neuronIndexMap);

var morphologyNeurons = 0;

for (var neuronIndex = 0;
     neuronIndex < morphologyTable.Count;
     neuronIndex++)
{
    if (morphologyTable.HasStatistics(
            neuronIndex))
    {
        morphologyNeurons++;
    }
}

Console.WriteLine(
    $"Morphology metadata: " +
    $"{morphologyNeurons:N0} / " +
    $"{morphologyTable.Count:N0}");

//
// Structural connectome
//

Console.WriteLine();
Console.WriteLine(
    "Building compact connectome...");

var connectomeBuildTimer =
    Stopwatch.StartNew();

var connectome =
    CompactConnectomeBuilder.Build(
        neuronIndexMap,
        () => dataset.ReadConnections());

connectomeBuildTimer.Stop();

Console.WriteLine();
Console.WriteLine(
    "Compact connectome built.");

Console.WriteLine(
    $"Neurons:     " +
    $"{connectome.NeuronCount:N0}");

Console.WriteLine(
    $"Connections: " +
    $"{connectome.ConnectionCount:N0}");

Console.WriteLine(
    $"Neuropils:   " +
    $"{connectome.NeuropilCount:N0}");

Console.WriteLine(
    $"Build time:  " +
    $"{connectomeBuildTimer.Elapsed.TotalSeconds:F2} s");

//
// Connection neurotransmitter statistics
//

Console.WriteLine();
Console.WriteLine(
    "Connection neurotransmitter types:");

var connectionTypeCounts =
    new Dictionary<
        NeurotransmitterType,
        long>();

var representedSynapsesByType =
    new Dictionary<
        NeurotransmitterType,
        long>();

foreach (var type in
         Enum.GetValues<NeurotransmitterType>())
{
    connectionTypeCounts[type] = 0;
    representedSynapsesByType[type] = 0;
}

for (var neuronIndex = 0;
     neuronIndex < connectome.NeuronCount;
     neuronIndex++)
{
    var neurotransmitterTypes =
        connectome.GetNeurotransmitterTypes(
            neuronIndex);

    var synapseCounts =
        connectome.GetSynapseCounts(
            neuronIndex);

    for (var connectionIndex = 0;
         connectionIndex <
         neurotransmitterTypes.Length;
         connectionIndex++)
    {
        var type =
            neurotransmitterTypes[
                connectionIndex];

        connectionTypeCounts[type]++;

        representedSynapsesByType[type] +=
            synapseCounts[
                connectionIndex];
    }
}

foreach (var type in
         Enum.GetValues<NeurotransmitterType>())
{
    Console.WriteLine(
        $"  {type,-16} " +
        $"{connectionTypeCounts[type],10:N0} connections  " +
        $"{representedSynapsesByType[type],12:N0} synapses");
}

//
// Neuropil list
//

Console.WriteLine();
Console.WriteLine("Neuropils:");

for (var neuropilIndex = 0;
     neuropilIndex < connectome.NeuropilCount;
     neuropilIndex++)
{
    Console.WriteLine(
        $"  {neuropilIndex,3}: " +
        $"{connectome.GetNeuropilName((ushort)neuropilIndex)}");
}

//
// Neuron-level predicted neurotransmitter statistics
//

Console.WriteLine();
Console.WriteLine(
    "Predicted neuron neurotransmitter types:");

var neuronTypeCounts =
    neurons
        .GroupBy(
            neuron =>
                string.IsNullOrWhiteSpace(
                    neuron.NeurotransmitterType)
                    ? "Unknown"
                    : neuron.NeurotransmitterType)
        .OrderByDescending(
            group => group.Count());

foreach (var group in neuronTypeCounts)
{
    Console.WriteLine(
        $"  {group.Key,-12} " +
        $"{group.Count(),10:N0}");
}

//
// Neural simulation
//

Console.WriteLine();
Console.WriteLine(
    "Building postsynaptic input model...");

var postsynapticInputs =
    PostsynapticInputTable.Build(
        connectome);

Console.WriteLine(
    $"Postsynaptic input totals: " +
    $"{postsynapticInputs.Count:N0} neurons");

Console.WriteLine();
Console.WriteLine(
    "Initialising full-brain neural simulation...");

var neuralParameters =
    LifNeuronParameters.Default;

var neuralState =
    new NeuronStateTable(
        connectome.NeuronCount,
        neuralParameters.RestingPotentialMv);

var neuralModel =
    new LifNeuronModel(
        neuralParameters);

var synapticEffectModel =
    new FastTransmitterSynapticEffectModel(
        postsynapticInputs,
        fullInputDriveMv: 40f);

var neuralSimulation =
    new NeuralSimulation(
        connectome,
        neuralState,
        neuralModel,
        synapticEffectModel);

Console.WriteLine(
    $"Neural states: " +
    $"{neuralState.Count:N0}");

//
// First whole-brain timestep at rest
//

Console.WriteLine();
Console.WriteLine(
    "Running first whole-brain timestep...");

var neuralTimer =
    Stopwatch.StartNew();

neuralSimulation.Step(
    timeStepMs: 1f);

neuralTimer.Stop();

Console.WriteLine(
    $"Simulation time: " +
    $"{neuralSimulation.SimulationTimeMs:F1} ms");

Console.WriteLine(
    $"Wall time: " +
    $"{neuralTimer.Elapsed.TotalMilliseconds:F2} ms");

//
// Stimulate one real neuron
//

Console.WriteLine();
Console.WriteLine(
    "Stimulating a real FAFB neuron...");

var stimulatedNeuronIndex =
    FindCholinergicStimulusNeuron(
        connectome);

var stimulatedRootId =
    neuronIndexMap.GetRootId(
        stimulatedNeuronIndex);

var stimulatedName =
    identityTable.GetName(
        stimulatedNeuronIndex)
    ?? "(unnamed)";

var stimulatedType =
    identityTable.GetPrimaryType(
        stimulatedNeuronIndex)
    ?? "(untyped)";

var stimulatedSide =
    identityTable.GetSide(
        stimulatedNeuronIndex)
    ?? "(unknown)";

var outgoingTargets =
    connectome.GetPostsynapticIndices(
        stimulatedNeuronIndex);

var outgoingSynapseCounts =
    connectome.GetSynapseCounts(
        stimulatedNeuronIndex);

var totalOutgoingSynapses = 0L;

foreach (var synapseCount in
         outgoingSynapseCounts)
{
    totalOutgoingSynapses +=
        synapseCount;
}

Console.WriteLine(
    $"Neuron index:     " +
    $"{stimulatedNeuronIndex:N0}");

Console.WriteLine(
    $"Root ID:          " +
    $"{stimulatedRootId}");

Console.WriteLine(
    $"Name:             " +
    $"{stimulatedName}");

Console.WriteLine(
    $"Primary type:     " +
    $"{stimulatedType}");

Console.WriteLine(
    $"Side:             " +
    $"{stimulatedSide}");

Console.WriteLine(
    $"Connections:      " +
    $"{outgoingTargets.Length:N0}");

Console.WriteLine(
    $"Outgoing synapses: " +
    $"{totalOutgoingSynapses:N0}");

//
// The default reference LIF model begins at -60 mV and has a
// threshold of -45 mV.
//
// With a 20 ms membrane time constant and 1 ms timestep, this
// artificial 400 mV drive forces the neuron over threshold.
//
// This is only a diagnostic stimulus. It does not represent a
// biologically measured sensory or synaptic input.
//

neuralState.AddSynapticDriveMv(
    stimulatedNeuronIndex,
    400f);

var stimulusTimer =
    Stopwatch.StartNew();

neuralSimulation.Step(
    timeStepMs: 1f);

stimulusTimer.Stop();

Console.WriteLine();
Console.WriteLine(
    $"Stimulated neuron fired: " +
    $"{neuralState.DidFire(stimulatedNeuronIndex)}");

Console.WriteLine(
    $"Simulation time: " +
    $"{neuralSimulation.SimulationTimeMs:F1} ms");

Console.WriteLine(
    $"Wall time: " +
    $"{stimulusTimer.Elapsed.TotalMilliseconds:F2} ms");

//
// Determine which real FAFB neurons received fast synaptic drive
// from the stimulated neuron.
//

var uniqueTargets =
    new HashSet<int>();

foreach (var targetIndex in
         outgoingTargets)
{
    uniqueTargets.Add(
        targetIndex);
}

var affectedTargets =
    new List<(int Index, float Drive)>();

foreach (var targetIndex in
         uniqueTargets)
{
    var drive =
        neuralState.GetSynapticDriveMv(
            targetIndex);

    if (drive != 0f)
    {
        affectedTargets.Add(
            (
                targetIndex,
                drive
            ));
    }
}

affectedTargets.Sort(
    (left, right) =>
        MathF.Abs(right.Drive)
            .CompareTo(
                MathF.Abs(left.Drive)));

Console.WriteLine();
Console.WriteLine(
    $"Unique outgoing targets: " +
    $"{uniqueTargets.Count:N0}");

Console.WriteLine(
    $"Postsynaptic neurons receiving fast drive: " +
    $"{affectedTargets.Count:N0}");

Console.WriteLine();
Console.WriteLine(
    "Strongest queued postsynaptic effects:");

if (affectedTargets.Count == 0)
{
    Console.WriteLine(
        "  No fast postsynaptic effects were generated.");
}
else
{
    foreach (var target in
             affectedTargets.Take(10))
    {
        var rootId =
            neuronIndexMap.GetRootId(
                target.Index);

        var name =
            identityTable.GetName(
                target.Index)
            ?? "(unnamed)";

        var primaryType =
            identityTable.GetPrimaryType(
                target.Index)
            ?? "(untyped)";

        Console.WriteLine(
            $"  {target.Index,7:N0}  " +
            $"{target.Drive,10:F4} mV  " +
            $"{rootId}  " +
            $"{name}  " +
            $"[{primaryType}]");
    }
}

//
// Supplementary dataset smoke tests
//

Console.WriteLine();
Console.WriteLine(
    "Checking supplementary datasets...");

var columnAssignmentCount =
    dataset
        .ReadColumnAssignments()
        .Count();

Console.WriteLine(
    $"  Column assignments: " +
    $"{columnAssignmentCount:N0}");

var connectivityTagCount =
    dataset
        .ReadConnectivityTags()
        .Count();

Console.WriteLine(
    $"  Connectivity tags: " +
    $"{connectivityTagCount:N0}");

var processedLabelCount =
    dataset
        .ReadProcessedLabels()
        .Count();

Console.WriteLine(
    $"  Processed labels: " +
    $"{processedLabelCount:N0}");

var visualNeuronTypeCount =
    dataset
        .ReadVisualNeuronTypes()
        .Count();

Console.WriteLine(
    $"  Visual neuron types: " +
    $"{visualNeuronTypeCount:N0}");

var synapseTableReadable =
    dataset
        .ReadSynapses()
        .Any();

Console.WriteLine(
    $"  Individual synapse table readable: " +
    $"{synapseTableReadable}");

Console.WriteLine();
Console.WriteLine(
    "FAFB v783 import and neural smoke test completed successfully.");

static int FindCholinergicStimulusNeuron(
    CompactConnectome connectome)
{
    var bestNeuronIndex =
        -1;

    var bestCholinergicConnectionCount =
        -1;

    for (var neuronIndex = 0;
         neuronIndex < connectome.NeuronCount;
         neuronIndex++)
    {
        var neurotransmitters =
            connectome.GetNeurotransmitterTypes(
                neuronIndex);

        if (neurotransmitters.Length == 0)
        {
            continue;
        }

        var cholinergicConnections =
            0;

        foreach (var neurotransmitter
                 in neurotransmitters)
        {
            if (neurotransmitter ==
                NeurotransmitterType.Acetylcholine)
            {
                cholinergicConnections++;
            }
        }

        if (cholinergicConnections >
            bestCholinergicConnectionCount)
        {
            bestCholinergicConnectionCount =
                cholinergicConnections;

            bestNeuronIndex =
                neuronIndex;
        }
    }

    if (bestNeuronIndex < 0)
    {
        throw new InvalidOperationException(
            "No cholinergic neuron with outgoing connections was found.");
    }

    return bestNeuronIndex;
}

static DirectoryInfo FindRepositoryRoot()
{
    var directory =
        new DirectoryInfo(
            Directory.GetCurrentDirectory());

    while (directory is not null)
    {
        var solutionPath =
            Path.Combine(
                directory.FullName,
                "flyDoom.slnx");

        if (File.Exists(solutionPath))
        {
            return directory;
        }

        directory =
            directory.Parent;
    }

    throw new DirectoryNotFoundException(
        "Could not locate the FlyDoom repository root.");
}