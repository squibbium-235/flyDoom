using FlyDoom.Connectome.Build;
using FlyDoom.Connectome.Import;
using FlyDoom.Connectome.Model;
using FlyDoom.Core.Biology;
using System.Diagnostics;

var repoRoot = FindRepositoryRoot();

var dataDirectory =
    Path.Combine(
        repoRoot.FullName,
        "data",
        "fafb-v783",
        "raw");

Console.WriteLine($"Repository: {repoRoot.FullName}");
Console.WriteLine($"Data:       {dataDirectory}");
Console.WriteLine();

Console.WriteLine("FlyDoom FAFB v783");
Console.WriteLine("=================");
Console.WriteLine();

var dataset =
    new FafbDatasetReader(dataDirectory);

Console.WriteLine("Loading neurons...");

var neurons =
    dataset.ReadNeurons().ToList();

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
Console.WriteLine("Building neuron metadata...");

var neuronTable =
    CompactNeuronTableBuilder.Build(
        neurons,
        neuronIndexMap);

Console.WriteLine(
    $"Neuron metadata: {neuronTable.Count:N0}");

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
    $"Neuron identities: {identityTable.Count:N0}");

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
Console.WriteLine("Identity metadata coverage:");

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

Console.WriteLine();
Console.WriteLine("Building neuron positions...");

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

Console.WriteLine();
Console.WriteLine(
    "Building compact connectome...");

var buildTimer =
    Stopwatch.StartNew();

var connectome =
    CompactConnectomeBuilder.Build(
        neuronIndexMap,
        () => dataset.ReadConnections());

buildTimer.Stop();

Console.WriteLine();
Console.WriteLine(
    "Compact connectome built.");

Console.WriteLine(
    $"Neurons:     " +
    $"{connectome.NeuronCount:N0}");

Console.WriteLine(
    $"Connections: " +
    $"{connectome.ConnectionCount:N0}");

Console.WriteLine();
Console.WriteLine(
    "Connection neurotransmitter types:");

var connectionTypeCounts =
    new Dictionary<NeurotransmitterType, long>();

var representedSynapsesByType =
    new Dictionary<NeurotransmitterType, long>();

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

Console.WriteLine();
Console.WriteLine(
    $"Neuropils: {connectome.NeuropilCount:N0}");

Console.WriteLine();
Console.WriteLine("Neuropils:");

for (var i = 0;
     i < connectome.NeuropilCount;
     i++)
{
    Console.WriteLine(
        $"  {i,3}: " +
        $"{connectome.GetNeuropilName((ushort)i)}");
}

Console.WriteLine();
Console.WriteLine(
    $"Connectome build time: " +
    $"{buildTimer.Elapsed.TotalSeconds:F2} s");

Console.WriteLine();
Console.WriteLine(
    "Predicted neuron neurotransmitter types:");

var neuronTypeCounts =
    neurons
        .GroupBy(
            neuron =>
                neuron.NeurotransmitterType
                ?? "Unknown")
        .OrderByDescending(
            group => group.Count());

foreach (var group in neuronTypeCounts)
{
    Console.WriteLine(
        $"  {group.Key,-12} " +
        $"{group.Count(),10:N0}");
}

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
    "FAFB v783 import completed successfully.");

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