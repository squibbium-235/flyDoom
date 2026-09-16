using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using FlyDoom.Neural.Simulation;
using FlyDoom.Runtime;
using FlyDoom.Vision.Model;

namespace FlyDoom.Gui;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _runTimer;

    private FlyDoomRuntime? _runtime;

    private VisualColumn? _stimulusColumn;

    private NeuralStepStatistics? _lastStep;

    private int? _selectedNeuronIndex;

    private int[] _descendingNeuronIndices =
        [];

    private double _lastStepWallTimeMs;

    private int _lastAdvancedSimulationMs;

    public MainWindow()
    {
        InitializeComponent();

        //
        // Keep simulation execution deliberately slower than the rendering
        // refresh rate for now. One timer tick may advance several biological
        // milliseconds depending on the selected control.
        //

        _runTimer =
            new DispatcherTimer
            {
                Interval =
                    TimeSpan.FromMilliseconds(
                        100)
            };

        _runTimer.Tick +=
            RunTimer_OnTick;

        Opened +=
            MainWindow_OnOpened;

        Closed +=
            MainWindow_OnClosed;

        BrainViewControl.NeuronSelected +=
            BrainViewControl_OnNeuronSelected;

        VisualFieldViewControl.ColumnSelected +=
            VisualFieldViewControl_OnColumnSelected;
    }

    private async void MainWindow_OnOpened(
        object? sender,
        EventArgs e)
    {
        Opened -=
            MainWindow_OnOpened;

        await LoadRuntimeAsync();
    }

    private void MainWindow_OnClosed(
        object? sender,
        EventArgs e)
    {
        _runTimer.Stop();
    }

    private async Task LoadRuntimeAsync()
    {
        try
        {
            StatusTextBlock.Text =
                "Loading FAFB v783...";

            var dataDirectory =
                RepositoryLocator
                    .GetFafbV783DataDirectory();

            //
            // Loading FAFB involves substantial CSV parsing and connectome
            // construction, so keep that work off Avalonia's UI thread.
            //

            var runtime =
                await Task.Run(
                    () =>
                        FlyDoomBootstrap
                            .LoadFafbV783(
                                dataDirectory));

            _runtime =
                runtime;

            _stimulusColumn =
                FindCentralColumn(
                    runtime.VisualColumns,
                    "right");

            _descendingNeuronIndices =
                FindDescendingNeuronIndices(
                    runtime);

            BrainViewControl.SetRuntime(
                runtime);

            VisualFieldViewControl.SetColumns(
                runtime.VisualColumns);

            VisualFieldViewControl
                .SetStimulatedColumn(
                    _stimulusColumn);

            FlashButton.IsEnabled =
                true;

            RunPauseButton.IsEnabled =
                true;

            StepButton.IsEnabled =
                true;

            AdvanceButton.IsEnabled =
                true;

            ResetViewButton.IsEnabled =
                true;

            StatusTextBlock.Text =
                $"Loaded " +
                $"{runtime.Connectome.NeuronCount:N0} neurons, " +
                $"{runtime.Connectome.ConnectionCount:N0} connections";

            UpdateStimulusDisplay();

            SimulationStatusTextBlock.Text =
                "Simulation ready at 0.0 ms.";

            UpdateActivityDisplay();
            UpdateSelectedNeuronDisplay();
            UpdateBehaviourDisplay();
        }
        catch (Exception exception)
        {
            StatusTextBlock.Text =
                "Failed to load FlyDoom.";

            ActivityListBox.ItemsSource =
                new[]
                {
                    new ActiveNeuronListItem(
                        -1,
                        0,
                        0,
                        false,
                        exception.Message,
                        "load error")
                };
        }
    }

    private void FlashButton_OnClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_runtime is null ||
            _stimulusColumn is null)
        {
            return;
        }

        var stimulus =
            _runtime
                .PhotoreceptorStimulator
                .ApplyColumnFlash(
                    _stimulusColumn,
                    _runtime.NeuralState,
                    intensity: 1f);

        StepSimulation(
            1);

        StimulusTextBlock.Text =
            $"Flash: {_stimulusColumn.Hemisphere} " +
            $"column {_stimulusColumn.ColumnId}. " +
            $"{stimulus.PhotoreceptorCount:N0} photoreceptors, " +
            $"{stimulus.UniqueTargetCount:N0} targets, " +
            $"strongest input " +
            $"{stimulus.MostNegativeInputAmplitudeMv:F2} mV-equiv.";
    }

    private void RunPauseButton_OnClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_runtime is null)
        {
            return;
        }

        if (_runTimer.IsEnabled)
        {
            _runTimer.Stop();

            RunPauseButton.Content =
                "Run";
        }
        else
        {
            _runTimer.Start();

            RunPauseButton.Content =
                "Pause";
        }
    }

    private void StepButton_OnClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        StepSimulation(
            1);
    }

    private void AdvanceButton_OnClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        StepSimulation(
            10);
    }

    private void ResetViewButton_OnClick(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        BrainViewControl.ResetView();
    }

    private void RunTimer_OnTick(
        object? sender,
        EventArgs e)
    {
        StepSimulation(
            GetRunStepsPerTick());
    }

    private void BrainViewControl_OnNeuronSelected(
        int neuronIndex)
    {
        SelectNeuron(
            neuronIndex,
            focusBrain: false);

        //
        // If the selected brain neuron also happens to be in the current
        // activity results, keep the list selection synchronised.
        //

        if (ActivityListBox.ItemsSource
            is IEnumerable<ActiveNeuronListItem> items)
        {
            var matchingItem =
                items.FirstOrDefault(
                    item =>
                        item.NeuronIndex ==
                        neuronIndex);

            if (matchingItem is not null)
            {
                ActivityListBox.SelectedItem =
                    matchingItem;
            }
        }
    }

    private void ActivityListBox_OnDoubleTapped(
        object? sender,
        TappedEventArgs e)
    {
        if (ActivityListBox.SelectedItem
            is not ActiveNeuronListItem item)
        {
            return;
        }

        if (item.NeuronIndex < 0)
        {
            return;
        }

        //
        // Double-clicking an activity result is a navigation action:
        //
        // 1. select the neuron,
        // 2. update its metadata inspector,
        // 3. zoom the 3D brain,
        // 4. centre the selected neuron.
        //

        SelectNeuron(
            item.NeuronIndex,
            focusBrain: true);

        e.Handled =
            true;
    }

    private void SelectNeuron(
        int neuronIndex,
        bool focusBrain)
    {
        _selectedNeuronIndex =
            neuronIndex;

        if (focusBrain)
        {
            BrainViewControl.FocusNeuron(
                neuronIndex);
        }
        else
        {
            BrainViewControl.SetSelectedNeuron(
                neuronIndex);
        }

        UpdateSelectedNeuronDisplay();
    }

    private void VisualFieldViewControl_OnColumnSelected(
        VisualColumn column)
    {
        _stimulusColumn =
            column;

        VisualFieldViewControl
            .SetStimulatedColumn(
                column);

        UpdateStimulusDisplay();
    }

    private void StepSimulation(
        int stepCount)
    {
        if (_runtime is null ||
            stepCount <= 0)
        {
            return;
        }

        var timer =
            Stopwatch.StartNew();

        for (var step = 0;
             step < stepCount;
             step++)
        {
            _lastStep =
                _runtime
                    .NeuralSimulation
                    .Step(
                        1f);
        }

        timer.Stop();

        _lastStepWallTimeMs =
            timer.Elapsed.TotalMilliseconds;

        _lastAdvancedSimulationMs =
            stepCount;

        RefreshViewer();
    }

    private void RefreshViewer()
    {
        BrainViewControl.RefreshActivity();

        UpdateActivityDisplay();
        UpdateSelectedNeuronDisplay();
        UpdateBehaviourDisplay();

        if (_lastStep is null)
        {
            return;
        }

        var step =
            _lastStep.Value;

        var realTimeFactor =
            _lastStepWallTimeMs <= 0
                ? 0
                : _lastAdvancedSimulationMs /
                  _lastStepWallTimeMs;

        SimulationStatusTextBlock.Text =
            $"t={step.SimulationTimeMs:F1} ms   " +
            $"fired={step.FiredNeuronCount:N0}   " +
            $"synaptic={step.ActiveSynapticNeuronCount:N0}   " +
            $"V={step.MinimumMembranePotentialMv:F2}.." +
            $"{step.MaximumMembranePotentialMv:F2} mV   " +
            $"{realTimeFactor:F2}× realtime";
    }

    private void UpdateStimulusDisplay()
    {
        if (_stimulusColumn is null)
        {
            StimulusTextBlock.Text =
                "No visual stimulus selected.";

            return;
        }

        StimulusTextBlock.Text =
            $"Selected: " +
            $"{_stimulusColumn.Hemisphere} " +
            $"column {_stimulusColumn.ColumnId}, " +
            $"P={_stimulusColumn.P:F1}, " +
            $"Q={_stimulusColumn.Q:F1}.";
    }

    private void UpdateSelectedNeuronDisplay()
    {
        if (_runtime is null ||
            _selectedNeuronIndex is null)
        {
            SelectedNeuronTextBlock.Text =
                "Click a neuron in the brain view.";

            return;
        }

        var neuronIndex =
            _selectedNeuronIndex.Value;

        var name =
            _runtime
                .IdentityTable
                .GetName(
                    neuronIndex)
            ?? "(unnamed)";

        var type =
            _runtime
                .IdentityTable
                .GetPrimaryType(
                    neuronIndex)
            ?? "(untyped)";

        var side =
            _runtime
                .IdentityTable
                .GetSide(
                    neuronIndex)
            ?? "unknown";

        var flow =
            _runtime
                .IdentityTable
                .GetFlow(
                    neuronIndex)
            ?? "unknown";

        var rootId =
            _runtime
                .NeuronIndexMap
                .GetRootId(
                    neuronIndex);

        var transmitter =
            _runtime
                .NeuronTable
                .GetNeurotransmitterType(
                    neuronIndex);

        var membranePotential =
            _runtime
                .NeuralState
                .GetMembranePotentialMv(
                    neuronIndex);

        var synapticInput =
            _runtime
                .NeuralState
                .GetSynapticInputMv(
                    neuronIndex);

        var fired =
            _runtime
                .NeuralState
                .DidFire(
                    neuronIndex);

        var outgoingConnections =
            _runtime
                .Connectome
                .GetPostsynapticIndices(
                    neuronIndex)
                .Length;

        SelectedNeuronTextBlock.Text =
            $"{name} [{type}]\n" +
            $"index {neuronIndex:N0}   root {rootId}\n" +
            $"side {side}   flow {flow}   NT {transmitter}\n" +
            $"V={membranePotential:F2} mV   " +
            $"input={synapticInput:F2} mV-equiv   " +
            $"fired={fired}\n" +
            $"{outgoingConnections:N0} outgoing connections";
    }

    private void UpdateActivityDisplay()
    {
        if (_runtime is null)
        {
            ActivityListBox.ItemsSource =
                Array.Empty<ActiveNeuronListItem>();

            return;
        }

        var strongest =
            FindStrongestActiveNeurons(
                _runtime,
                maximumCount: 50);

        ActivityListBox.ItemsSource =
            strongest;

        //
        // Keep the existing neuron selected in the activity list after a
        // simulation step rebuilds the item objects.
        //

        if (_selectedNeuronIndex is not null)
        {
            var selectedItem =
                strongest.FirstOrDefault(
                    item =>
                        item.NeuronIndex ==
                        _selectedNeuronIndex.Value);

            if (selectedItem is not null)
            {
                ActivityListBox.SelectedItem =
                    selectedItem;
            }
        }
    }

    private void UpdateBehaviourDisplay()
    {
        if (_runtime is null)
        {
            BehaviourTextBlock.Text =
                "Not loaded.";

            return;
        }

        if (_descendingNeuronIndices.Length == 0)
        {
            BehaviourTextBlock.Text =
                "No descending neurons were identified from the current " +
                "FAFB classifications. Semantic motor mapping will require " +
                "the BANC/VNC stage.";

            return;
        }

        var activeDescending =
            new List<ActiveNeuronListItem>();

        foreach (var neuronIndex in
                 _descendingNeuronIndices)
        {
            var input =
                _runtime
                    .NeuralState
                    .GetSynapticInputMv(
                        neuronIndex);

            var fired =
                _runtime
                    .NeuralState
                    .DidFire(
                        neuronIndex);

            if (!fired &&
                MathF.Abs(input) <=
                0.0001f)
            {
                continue;
            }

            activeDescending.Add(
                CreateActivityItem(
                    _runtime,
                    neuronIndex));
        }

        if (activeDescending.Count == 0)
        {
            BehaviourTextBlock.Text =
                $"{_descendingNeuronIndices.Length:N0} descending " +
                $"neurons identified; none currently active.\n" +
                $"Motor meaning is not assigned until BANC/VNC connectivity " +
                $"is incorporated.";

            return;
        }

        var strongest =
            activeDescending
                .OrderByDescending(
                    neuron =>
                        MathF.Abs(
                            neuron.Input))
                .Take(
                    5)
                .ToArray();

        var lines =
            new List<string>
            {
                $"{activeDescending.Count:N0} active descending neurons:"
            };

        foreach (var active in
                 strongest)
        {
            var side =
                _runtime
                    .IdentityTable
                    .GetSide(
                        active.NeuronIndex)
                ?? "?";

            lines.Add(
                $"{active.Name} ({side}) " +
                $"input={active.Input:F2}");
        }

        lines.Add(
            "Semantic motor mapping pending BANC/VNC.");

        BehaviourTextBlock.Text =
            string.Join(
                Environment.NewLine,
                lines);
    }

    private int GetRunStepsPerTick()
    {
        if (RunSpeedComboBox.SelectedItem
            is not ComboBoxItem item)
        {
            return 1;
        }

        if (item.Tag is not string tag ||
            !int.TryParse(
                tag,
                out var steps))
        {
            return 1;
        }

        return Math.Max(
            1,
            steps);
    }

    private static List<ActiveNeuronListItem>
        FindStrongestActiveNeurons(
            FlyDoomRuntime runtime,
            int maximumCount)
    {
        var active =
            new List<ActiveNeuronListItem>();

        for (var neuronIndex = 0;
             neuronIndex < runtime.NeuralState.Count;
             neuronIndex++)
        {
            var input =
                runtime
                    .NeuralState
                    .GetSynapticInputMv(
                        neuronIndex);

            var fired =
                runtime
                    .NeuralState
                    .DidFire(
                        neuronIndex);

            if (!fired &&
                MathF.Abs(input) <=
                0.0001f)
            {
                continue;
            }

            active.Add(
                CreateActivityItem(
                    runtime,
                    neuronIndex));
        }

        return active
            .OrderByDescending(
                neuron =>
                    MathF.Abs(
                        neuron.Input))
            .Take(
                maximumCount)
            .ToList();
    }

    private static ActiveNeuronListItem
        CreateActivityItem(
            FlyDoomRuntime runtime,
            int neuronIndex)
    {
        var name =
            runtime
                .IdentityTable
                .GetName(
                    neuronIndex)
            ?? "(unnamed)";

        var type =
            runtime
                .IdentityTable
                .GetPrimaryType(
                    neuronIndex)
            ?? "(untyped)";

        return new ActiveNeuronListItem(
            neuronIndex,
            runtime
                .NeuralState
                .GetSynapticInputMv(
                    neuronIndex),
            runtime
                .NeuralState
                .GetMembranePotentialMv(
                    neuronIndex),
            runtime
                .NeuralState
                .DidFire(
                    neuronIndex),
            name,
            type);
    }

    private static int[] FindDescendingNeuronIndices(
        FlyDoomRuntime runtime)
    {
        var descending =
            new List<int>();

        for (var neuronIndex = 0;
             neuronIndex < runtime.IdentityTable.Count;
             neuronIndex++)
        {
            var flow =
                runtime
                    .IdentityTable
                    .GetFlow(
                        neuronIndex);

            var superClass =
                runtime
                    .IdentityTable
                    .GetSuperClass(
                        neuronIndex);

            var neuronClass =
                runtime
                    .IdentityTable
                    .GetClass(
                        neuronIndex);

            if (ContainsDescending(
                    flow) ||
                ContainsDescending(
                    superClass) ||
                ContainsDescending(
                    neuronClass))
            {
                descending.Add(
                    neuronIndex);
            }
        }

        return descending.ToArray();
    }

    private static bool ContainsDescending(
        string? value)
    {
        return value?.Contains(
                   "descending",
                   StringComparison.OrdinalIgnoreCase)
               == true;
    }

    private static VisualColumn FindCentralColumn(
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

            if (!double.IsFinite(
                    column.P) ||
                !double.IsFinite(
                    column.Q))
            {
                continue;
            }

            var distanceSquared =
                column.P *
                column.P +
                column.Q *
                column.Q;

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
                $"No usable visual column was found for " +
                $"hemisphere '{hemisphere}'.");
    }
}