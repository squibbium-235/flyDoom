using FlyDoom.Connectome.IO;
using FlyDoom.Connectome.Import;
using FlyDoom.Connectome.Model;
using System.Diagnostics;
using FlyDoom.Connectome.Build;

// Start searching from the application's current working directory.
// Visual studio doesnt necessarily launch applications from the repo root, so relative paths would be unreliable
var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
DirectoryInfo? repoRoot = currentDirectory;

// Walk upwards through the dir tree until the solution file is found, that shows the root of the repo
while(repoRoot is not null && !File.Exists(Path.Combine(repoRoot.FullName, "flyDoom.slnx")))
{
    repoRoot = repoRoot.Parent;
}

// If the solution file isnt found, the application cant find where the data reliably is
if(repoRoot is null)
{
    Console.WriteLine("Could not find the FlyDoom repository root.");
    return;
}

// construct the path using Path.Combine rather than slash characters (heh like the guy from Guns N' Roses) so that shit works on all the OSes
var dataDirectory = Path.Combine(repoRoot.FullName, "data", "fafb-v783", "raw");

Console.WriteLine($"Repository: {repoRoot.FullName}");
Console.WriteLine($"Data:       {dataDirectory}");
Console.WriteLine();

// stop early if the directory hasnt been created/doesnt exist
if(!Directory.Exists(dataDirectory))
{
    Console.WriteLine("FAFB data directory does not exist.");
    return;
}

// only inspect g-zip files because the downloads are stored compressed
var files = Directory.GetFiles(dataDirectory, "*.gz");

Console.WriteLine($"Found {files.Length} compressed files.");
Console.WriteLine();

// read only the first line from each decompressed file
// this gives column headers without loading several gigabytes of files
foreach(var file in files)
{
    Console.WriteLine($"=== {Path.GetFileName(file)} ===");

    using var reader = GzipCsvFile.Open(file);

    Console.WriteLine(reader.ReadLine());
    Console.WriteLine();
}

var dataset = new FafbDatasetReader(dataDirectory);

Console.WriteLine("FlyDoom FAFB v783");
Console.WriteLine("=================");
Console.WriteLine();

Console.WriteLine("Loading neurons...");

// The neuron dataset contains *around* 139,000 records, which is small enough to hold comfortably in memory.
var neurons = dataset
    .ReadNeurons()
    .ToList();

// Convert the large, sparse FlyWire root IDs into compact zero-based indicies for efficiency
var neuronIndexMap = NeuronIndexMap.Create(
    neurons.Select(neuron => neuron.RootId));

Console.WriteLine();
Console.WriteLine("Building compact connectome...");

var buildTimer = Stopwatch.StartNew();
var connectome = CompactConnectomeBuilder.Build(neuronIndexMap, () => dataset.ReadConnections());

buildTimer.Stop();

Console.WriteLine();
Console.WriteLine("Compact connectome built.");
Console.WriteLine($"Neurons:     {connectome.NeuronCount:N0}");
Console.WriteLine($"Connections: {connectome.ConnectionCount:N0}");
Console.WriteLine($"Build Time:  {buildTimer.Elapsed.TotalSeconds:N2} s");

Console.WriteLine($"Neuron indices: 0 - {neuronIndexMap.Count - 1:N0}");
Console.WriteLine();

Console.WriteLine($"Neurons: {neurons.Count:N0}");
Console.WriteLine();

Console.WriteLine("Predicted neurotransmitter types:");
Console.WriteLine();

// Group neurons by their predicted neurotransmitter so that we can verify the dataset has been sensibly interpreted.
var neurotransmitterCounts = neurons
    .GroupBy(neuron =>
    string.IsNullOrWhiteSpace(neuron.NeurotransmitterType)
        ? "Unknown"
        : neuron.NeurotransmitterType)
    .OrderByDescending(group => group.Count());

foreach(var group in neurotransmitterCounts)
{
    Console.WriteLine($"  {group.Key,-12} {group.Count(),10:N0}");
}