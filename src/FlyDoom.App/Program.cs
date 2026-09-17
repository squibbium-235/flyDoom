using System.Diagnostics;
using FlyDoom.Connectome.Build;
using FlyDoom.Connectome.Import;
using FlyDoom.Connectome.Model;
using FlyDoom.Neural.Models;
using FlyDoom.Neural.Simulation;
using FlyDoom.Neural.Transmission;
using FlyDoom.Vision.Build;
using FlyDoom.Vision.Model;
using FlyDoom.Vision.Stimulation;

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
// The neuron list establishes the compact index space shared by the
// connectome, biological metadata, visual system, and simulation state.
//

Console.WriteLine(
    "Loading neurons...");

var neurons =
    dataset
        .ReadNeurons()
        .ToList();

var neuronIndexMap =
    NeuronIndexMap.Create(
        neurons.Select(
            neuron => neuron.RootId));

Console.WriteLine(
    $"Neurons: {neurons.Count:N0}");

//
// Build biological and identity metadata.
//

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

//
// Retain spatial and morphological information now even though the first
// visual stimulus does not yet use full neuron geometry.
//

Console.WriteLine();
Console.WriteLine(
    "Building neuron positions...");

var positionTable =
    CompactNeuronPositionTableBuilder.Build(
        () => dataset.ReadCoordinates(),
        neuronIndexMap);

Console.WriteLine(
    $"Coordinate records: " +
    $"{positionTable.PositionCount:N0}");

Console.WriteLine();
Console.WriteLine(
    "Building neuron morphology metadata...");

var morphologyTable =
    CompactNeuronMorphologyTableBuilder.Build(
        dataset.ReadCellStats(),
        neuronIndexMap);

var morphologyCount =
    0;

for (var neuronIndex = 0;
     neuronIndex < morphologyTable.Count;
     neuronIndex++)
{
    if (morphologyTable.HasStatistics(
            neuronIndex))
    {
        morphologyCount++;
    }
}

Console.WriteLine(
    $"Morphology metadata: " +
    $"{morphologyCount:N0} / " +
    $"{morphologyTable.Count:N0}");

//
// Build the visual-system annotation layer.
//
// The catalogue is neuron-centric. The column map then groups those
// neuron assignments into actual positions in visual space.
//

Console.WriteLine();
Console.WriteLine(
    "Building visual system...");

var visualCatalog =
    VisualNeuronCatalogBuilder.Build(
        neuronIndexMap,
        dataset.ReadVisualNeuronTypes(),
        dataset.ReadColumnAssignments());

var visualColumns =
    VisualColumnMapBuilder.Build(
        visualCatalog);

var visualNeuronCount =
    0;

for (var neuronIndex = 0;
     neuronIndex < visualCatalog.Count;
     neuronIndex++)
{
    if (visualCatalog.IsVisualNeuron(
            neuronIndex))
    {
        visualNeuronCount++;
    }
}

var leftColumnCount =
    visualColumns.Columns.Count(
        column =>
            column.Hemisphere.Equals(
                "left",
                StringComparison.OrdinalIgnoreCase));

var rightColumnCount =
    visualColumns.Columns.Count(
        column =>
            column.Hemisphere.Equals(
                "right",
                StringComparison.OrdinalIgnoreCase));

Console.WriteLine(
    $"Visual neurons: " +
    $"{visualNeuronCount:N0}");

Console.WriteLine(
    $"Spatial columns: " +
    $"{visualColumns.Count:N0}");

Console.WriteLine(
    $"  Left:  {leftColumnCount:N0}");

Console.WriteLine(
    $"  Right: {rightColumnCount:N0}");

//
// Build structural connectivity.
//
// The current filtered Princeton table contains 5.34 million aggregated
// connections representing roughly 50.7 million anatomical synapses.
//

Console.WriteLine();
Console.WriteLine(
    "Building compact connectome...");

var connectomeTimer =
    Stopwatch.StartNew();

var connectome =
    CompactConnectomeBuilder.Build(
        neuronIndexMap,
        () => dataset.ReadConnections());

connectomeTimer.Stop();

Console.WriteLine(
    $"Neurons:     " +
    $"{connectome.NeuronCount:N0}");

Console.WriteLine(
    $"Connections: " +
    $"{connectome.ConnectionCount:N0}");

Console.WriteLine(
    $"Build time:  " +
    $"{connectomeTimer.Elapsed.TotalSeconds:F2} s");

//
// Build total postsynaptic input counts.
//
// Both ordinary fast transmission and the first graded photoreceptor model
// use connection strength relative to a target neuron's total anatomical
// input rather than treating raw synapse count as a voltage.
//

Console.WriteLine();
Console.WriteLine(
    "Building postsynaptic input model...");

var postsynapticInputs =
    PostsynapticInputTable.Build(
        connectome);

//
// Initialise the reference neural dynamics.
//
// LIF remains a provisional model for the spiking portion of the simulator.
// Photoreceptors are handled separately below as graded sensory neurons.
//

Console.WriteLine();
Console.WriteLine(
    "Initialising neural simulation...");

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

Console.WriteLine(
    $"Neural states: " +
    $"{neuralState.Count:N0}");

//
// Establish a silent baseline before presenting visual input.
//

Console.WriteLine();
Console.WriteLine(
    "Running resting timestep...");

var restingResult =
    neuralSimulation.Step(
        timeStepMs: 1f);

Console.WriteLine(
    $"Neurons fired: " +
    $"{restingResult.FiredNeuronCount:N0}");

//
// Select the spatial column nearest the centre of the right visual field.
//
// P/Q are used for this diagnostic because they give us a convenient
// column-space coordinate system. Mapping these coordinates onto actual
// viewing angles and then onto DOOM pixels comes later.
//

var stimulusColumn =
    FindCentralColumn(
        visualColumns,
        hemisphere: "right");

Console.WriteLine();
Console.WriteLine(
    "Selected visual stimulus column:");

Console.WriteLine(
    $"  Hemisphere: {stimulusColumn.Hemisphere}");

Console.WriteLine(
    $"  Column ID:  {stimulusColumn.ColumnId}");

Console.WriteLine(
    $"  P/Q:        " +
    $"{stimulusColumn.P:F2}, " +
    $"{stimulusColumn.Q:F2}");

Console.WriteLine(
    $"  Neurons:    " +
    $"{stimulusColumn.NeuronCount:N0}");

//
// Show the sensory neurons that will actually receive the light stimulus.
//

Console.WriteLine();
Console.WriteLine(
    "Column photoreceptors:");

var columnPhotoreceptors =
    stimulusColumn
        .NeuronIndices
        .Where(
            neuronIndex =>
                IsInnerPhotoreceptor(
                    visualCatalog.GetType(
                        neuronIndex)))
        .ToArray();

foreach (var neuronIndex in
         columnPhotoreceptors)
{
    Console.WriteLine(
        $"  {visualCatalog.GetType(neuronIndex),-3} " +
        $"{identityTable.GetName(neuronIndex) ?? "(unnamed)",-20} " +
        $"{neuronIndexMap.GetRootId(neuronIndex)}");
}

//
// Present the first spatially-addressed visual stimulus.
//
// Photoreceptors are non-spiking graded neurons, so this does NOT force
// R7/R8 through the LIF threshold. Instead, the flash produces graded
// histaminergic input directly through their real FAFB connections.
//

Console.WriteLine();
Console.WriteLine(
    "Applying full-intensity column flash...");

var photoreceptorStimulator =
    new ColumnPhotoreceptorStimulator(
        visualCatalog,
        connectome,
        postsynapticInputs,
        fullHistamineInputAmplitudeMv: 40f);

var stimulusResult =
    photoreceptorStimulator.ApplyColumnFlash(
        stimulusColumn,
        neuralState,
        intensity: 1f);

Console.WriteLine(
    $"Photoreceptors stimulated: " +
    $"{stimulusResult.PhotoreceptorCount:N0}");

Console.WriteLine(
    $"Connections carrying sensory input: " +
    $"{stimulusResult.AppliedConnectionCount:N0}");

Console.WriteLine(
    $"Unique postsynaptic targets: " +
    $"{stimulusResult.UniqueTargetCount:N0}");

Console.WriteLine(
    $"Strongest immediate inhibitory input: " +
    $"{stimulusResult.MostNegativeInputAmplitudeMv:F4} mV-equiv");

//
// Follow the response after the brief flash.
//
// Minimum membrane voltage is important here because light-driven histamine
// initially hyperpolarises first-order targets. Maximum voltage alone would
// completely hide that response behind untouched neurons sitting at rest.
//

Console.WriteLine();
Console.WriteLine(
    "Visual response:");

Console.WriteLine();

Console.WriteLine(
    $"{"Time",7} " +
    $"{"Fired",8} " +
    $"{"Synaptic",10} " +
    $"{"Max input",12} " +
    $"{"Min V",9} " +
    $"{"Max V",9}");

Console.WriteLine(
    new string('-', 68));

for (var step = 0;
     step < 20;
     step++)
{
    var result =
        neuralSimulation.Step(
            timeStepMs: 1f);

    PrintStep(
        result);
}

Console.WriteLine();
Console.WriteLine(
    "First spatial visual stimulus completed successfully.");

static bool IsInnerPhotoreceptor(
    string? type)
{
    return string.Equals(
               type,
               "R7",
               StringComparison.OrdinalIgnoreCase) ||
           string.Equals(
               type,
               "R8",
               StringComparison.OrdinalIgnoreCase);
}

static VisualColumn FindCentralColumn(
    VisualColumnMap columns,
    string hemisphere)
{
    VisualColumn? bestColumn =
        null;

    var bestDistanceSquared =
        double.PositiveInfinity;

    foreach (var column in
             columns.Columns)
    {
        if (!column.Hemisphere.Equals(
                hemisphere,
                StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!double.IsFinite(column.P) ||
            !double.IsFinite(column.Q))
        {
            continue;
        }

        var distanceSquared =
            column.P * column.P +
            column.Q * column.Q;

        if (distanceSquared >=
            bestDistanceSquared)
        {
            continue;
        }

        bestDistanceSquared =
            distanceSquared;

        bestColumn =
            column;
    }

    return bestColumn
        ?? throw new InvalidOperationException(
            $"No visual column with finite P/Q coordinates " +
            $"was found for hemisphere '{hemisphere}'.");
}

static void PrintStep(
    NeuralStepStatistics result)
{
    Console.WriteLine(
        $"{result.SimulationTimeMs,7:F1} " +
        $"{result.FiredNeuronCount,8:N0} " +
        $"{result.ActiveSynapticNeuronCount,10:N0} " +
        $"{result.MaximumAbsoluteSynapticInputMv,12:F4} " +
        $"{result.MinimumMembranePotentialMv,9:F3} " +
        $"{result.MaximumMembranePotentialMv,9:F3}");
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