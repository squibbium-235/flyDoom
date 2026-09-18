using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FlyDoom.Neural.Simulation;
using FlyDoom.Runtime;
using FlyDoom.Vision.Diagnostics;
using FlyDoom.Vision.Model;
using FlyDoom.Vision.Stimulation;

namespace FlyDoom.Gui;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _runTimer;

    private FlyDoomRuntime? _runtime;

    private VisualPathwayMonitor? _visualPathwayMonitor;

    private MotionSweepRecorder? _motionSweepRecorder;

    private MotionSweepResult? _increasingMotionResult;

    private MotionSweepResult? _decreasingMotionResult;

    private VisualColumn? _stimulusColumn;

    private VisualFieldFrame? _visualFrame;

    private NeuralStepStatistics? _lastStep;

    private int? _selectedNeuronIndex;

    private int[] _descendingNeuronIndices =
        [];

    private double _lastStepWallTimeMs;

    private int _lastAdvancedSimulationMs;

    private bool _liveStimulus;

    private double _movingEdgeP;

    private bool _motionSweepActive;

    private MotionSweepDirection _activeMotionSweepDirection;

    public MainWindow()
    {
        InitializeComponent();

        //
        // Keep simulation execution deliberately slower than the rendering
        // refresh rate for now. One timer tick may advance several biological
        // milliseconds depending on the selected run speed.
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

        ApplyWorkspaceLayout();

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

            var runtime =
                await Task.Run(
                    () =>
                        FlyDoomBootstrap
                            .LoadFafbV783(
                                dataDirectory));

            _runtime =
                runtime;

            _visualPathwayMonitor =
                new VisualPathwayMonitor(
                    runtime.VisualCatalog);

            _motionSweepRecorder =
                new MotionSweepRecorder(
                    runtime.VisualCatalog);

            _stimulusColumn =
                FindCentralColumn(
                    runtime.VisualColumns,
                    "right");

            _movingEdgeP =
                _stimulusColumn.P;

            _descendingNeuronIndices =
                FindDescendingNeuronIndices(
                    runtime);

            BrainViewControl.SetRuntime(
                runtime);

            VisualFieldViewControl.SetColumns(
                runtime.VisualColumns);

            VisualFieldViewControl.SetStimulatedColumn(
                _stimulusColumn);

            RebuildVisualFrame();

            FlashButton.IsEnabled =
                true;

            LiveStimulusToggleButton.IsEnabled =
                true;

            RunPauseButton.IsEnabled =
                true;

            StepButton.IsEnabled =
                true;

            AdvanceButton.IsEnabled =
                true;

            ResetViewButton.IsEnabled =
                true;

            SweepIncreasingButton.IsEnabled =
                true;

            SweepDecreasingButton.IsEnabled =
                true;

            ClearMotionResultsButton.IsEnabled =
                true;

            StatusTextBlock.Text =
                $"Loaded " +
                $"{runtime.Connectome.NeuronCount:N0} neurons, " +
                $"{runtime.Connectome.ConnectionCount:N0} connections";

            SimulationStatusTextBlock.Text =
                "t=0.0 ms";

            UpdateActivityDisplay();
            UpdatePathwayDisplay();
            UpdateMotionDisplay();
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

    private void LayoutModeComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (WorkspaceGrid is null ||
            BrainPanel is null ||
            DiagnosticsPanel is null)
        {
            return;
        }

        ApplyWorkspaceLayout();
    }

    private void ApplyWorkspaceLayout()
    {
        var stacked =
            LayoutModeComboBox.SelectedItem
                is ComboBoxItem item &&
            item.Tag is string tag &&
            string.Equals(
                tag,
                "stacked",
                StringComparison.OrdinalIgnoreCase);

        WorkspaceGrid.ColumnDefinitions.Clear();
        WorkspaceGrid.RowDefinitions.Clear();

        if (stacked)
        {
            WorkspaceGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        new GridLength(
                            1,
                            GridUnitType.Star)
                });

            WorkspaceGrid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        new GridLength(
                            3,
                            GridUnitType.Star)
                });

            WorkspaceGrid.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        new GridLength(
                            2,
                            GridUnitType.Star)
                });

            WorkspaceGrid.ColumnSpacing =
                0;

            WorkspaceGrid.RowSpacing =
                12;

            Grid.SetColumn(
                BrainPanel,
                0);

            Grid.SetRow(
                BrainPanel,
                0);

            Grid.SetColumn(
                DiagnosticsPanel,
                0);

            Grid.SetRow(
                DiagnosticsPanel,
                1);

            return;
        }

        WorkspaceGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    new GridLength(
                        3,
                        GridUnitType.Star)
            });

        WorkspaceGrid.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width =
                    new GridLength(
                        2,
                        GridUnitType.Star)
            });

        WorkspaceGrid.RowDefinitions.Add(
            new RowDefinition
            {
                Height =
                    new GridLength(
                        1,
                        GridUnitType.Star)
            });

        WorkspaceGrid.ColumnSpacing =
            12;

        WorkspaceGrid.RowSpacing =
            0;

        Grid.SetColumn(
            BrainPanel,
            0);

        Grid.SetRow(
            BrainPanel,
            0);

        Grid.SetColumn(
            DiagnosticsPanel,
            1);

        Grid.SetRow(
            DiagnosticsPanel,
            0);
    }

    private void StimulusModeComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_motionSweepActive &&
            GetStimulusMode() !=
            "moving-edge")
        {
            CancelMotionSweep(
                "Sweep cancelled because the stimulus mode changed.");
        }

        if (_stimulusColumn is not null)
        {
            _movingEdgeP =
                _stimulusColumn.P;
        }

        RebuildVisualFrame();
    }

    private void MotionControl_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_motionSweepActive)
        {
            return;
        }

        RebuildVisualFrame();
    }

    private void MotionPolarityComboBox_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (_runtime is null ||
            _motionSweepActive)
        {
            return;
        }

        _increasingMotionResult =
            null;

        _decreasingMotionResult =
            null;

        RebuildVisualFrame();
        UpdateMotionDisplay();
    }

    private void LiveStimulusToggleButton_OnIsCheckedChanged(
        object? sender,
        RoutedEventArgs e)
    {
        _liveStimulus =
            LiveStimulusToggleButton.IsChecked ==
            true;

        if (!_liveStimulus &&
            _motionSweepActive)
        {
            CancelMotionSweep(
                "Sweep cancelled.");
        }
    }

    private void FlashButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_runtime is null ||
            _visualFrame is null)
        {
            return;
        }

        var stimulus =
            _runtime
                .PhotoreceptorStimulator
                .ApplyFrame(
                    _visualFrame,
                    _runtime.NeuralState);

        StepSimulation(
            1,
            applyLiveStimulus: false);

        StimulusTextBlock.Text =
            $"{GetStimulusModeName()}: " +
            $"{stimulus.ActiveColumnCount:N0} columns, " +
            $"{stimulus.PhotoreceptorCount:N0} photoreceptors, " +
            $"{stimulus.UniqueTargetCount:N0} targets, " +
            $"strongest input " +
            $"{stimulus.MostNegativeInputAmplitudeMv:F2} mV-equiv.";
    }

    private void RunPauseButton_OnClick(
        object? sender,
        RoutedEventArgs e)
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
        RoutedEventArgs e)
    {
        StepSimulation(
            1);
    }

    private void AdvanceButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        StepSimulation(
            10);
    }

    private void ResetViewButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        BrainViewControl.ResetView();
    }

    private void SweepIncreasingButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        StartMotionSweep(
            MotionSweepDirection.PIncreasing);
    }

    private void SweepDecreasingButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        StartMotionSweep(
            MotionSweepDirection.PDecreasing);
    }

    private void ClearMotionResultsButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_motionSweepActive)
        {
            CancelMotionSweep(
                "Sweep cancelled and results cleared.");
        }

        _increasingMotionResult =
            null;

        _decreasingMotionResult =
            null;

        UpdateMotionDisplay();
    }

    private void StartMotionSweep(
        MotionSweepDirection direction)
    {
        if (_runtime is null ||
            _motionSweepRecorder is null ||
            _stimulusColumn is null)
        {
            return;
        }

        if (_motionSweepActive)
        {
            CancelMotionSweep(
                "Previous sweep cancelled.");
        }

        _runTimer.Stop();

        RunPauseButton.Content =
            "Run";

        //
        // Both directions must begin from exactly the same transient neural
        // state. Structural connectivity and future learned state remain
        // untouched.
        //

        _runtime.ResetDynamicState();

        _lastStep =
            null;

        _lastStepWallTimeMs =
            0;

        _lastAdvancedSimulationMs =
            0;

        BrainViewControl.RefreshActivity();

        StimulusModeComboBox.SelectedIndex =
            4;

        _activeMotionSweepDirection =
            direction;

        MotionDirectionComboBox.SelectedIndex =
            direction ==
            MotionSweepDirection.PIncreasing
                ? 0
                : 1;

        var bounds =
            GetStimulusHemispherePBounds();

        _movingEdgeP =
            direction ==
            MotionSweepDirection.PIncreasing
                ? bounds.Minimum
                : bounds.Maximum;

        _motionSweepRecorder.Begin(
            direction,
            GetMotionEdgePolarity(),
            _movingEdgeP);

        _motionSweepActive =
            true;

        SetMotionExperimentControlsEnabled(
            false);

        LiveStimulusToggleButton.IsChecked =
            true;

        _liveStimulus =
            true;

        RebuildVisualFrame();

        MotionExperimentTextBlock.Text =
            $"Recording {GetMotionEdgePolarityName()} edge, " +
            $"{GetMotionDirectionName(direction)}...";

        _runTimer.Start();

        RunPauseButton.Content =
            "Pause";
    }

    private void CompleteMotionSweep()
    {
        if (!_motionSweepActive ||
            _motionSweepRecorder is null)
        {
            return;
        }

        _motionSweepActive =
            false;

        var result =
            _motionSweepRecorder.Complete();

        if (result.Direction ==
            MotionSweepDirection.PIncreasing)
        {
            _increasingMotionResult =
                result;
        }
        else
        {
            _decreasingMotionResult =
                result;
        }

        _runTimer.Stop();

        RunPauseButton.Content =
            "Run";

        LiveStimulusToggleButton.IsChecked =
            false;

        _liveStimulus =
            false;

        SetMotionExperimentControlsEnabled(
            true);

        UpdateMotionDisplay();
    }

    private void CancelMotionSweep(
        string message)
    {
        if (!_motionSweepActive)
        {
            return;
        }

        _motionSweepActive =
            false;

        _motionSweepRecorder?.Cancel();

        _runTimer.Stop();

        RunPauseButton.Content =
            "Run";

        _liveStimulus =
            false;

        SetMotionExperimentControlsEnabled(
            true);

        MotionExperimentTextBlock.Text =
            message;
    }

    private void SetMotionExperimentControlsEnabled(
        bool enabled)
    {
        SweepIncreasingButton.IsEnabled =
            enabled;

        SweepDecreasingButton.IsEnabled =
            enabled;

        MotionPolarityComboBox.IsEnabled =
            enabled;

        MotionSweepSpeedComboBox.IsEnabled =
            enabled;

        MotionDirectionComboBox.IsEnabled =
            enabled;

        MotionSpeedComboBox.IsEnabled =
            enabled;
    }

    private void RunTimer_OnTick(
        object? sender,
        EventArgs e)
    {
        StepSimulation(
            1);
    }

    private void BrainViewControl_OnNeuronSelected(
        int neuronIndex)
    {
        SelectNeuron(
            neuronIndex,
            focusBrain: false);

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
            is not ActiveNeuronListItem item ||
            item.NeuronIndex < 0)
        {
            return;
        }

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
        if (_motionSweepActive)
        {
            return;
        }

        _stimulusColumn =
            column;

        _movingEdgeP =
            column.P;

        VisualFieldViewControl.SetStimulatedColumn(
            column);

        RebuildVisualFrame();
    }

    private void StepSimulation(
        int stepCount,
        bool applyLiveStimulus = true)
    {
        if (_runtime is null ||
            stepCount <= 0)
        {
            return;
        }

        var timer =
            Stopwatch.StartNew();

        var completedSweep =
            false;

        var completedSteps =
            0;

        for (var stepIndex = 0;
             stepIndex < stepCount;
             stepIndex++)
        {
            VisualFrameStimulusStatistics? frameStimulus =
                null;

            //
            // This P value corresponds to the frame being presented during
            // this timestep. The edge is advanced only after recording it.
            //

            var presentedEdgeP =
                _movingEdgeP;

            if (applyLiveStimulus &&
                _liveStimulus &&
                _visualFrame is not null)
            {
                const float synapticTimeConstantMs =
                    5f;

                const float simulationStepMs =
                    1f;

                var sustainedDriveScale =
                    1f -
                    MathF.Exp(
                        -simulationStepMs /
                        synapticTimeConstantMs);

                frameStimulus =
                    _runtime
                        .PhotoreceptorStimulator
                        .ApplyFrame(
                            _visualFrame,
                            _runtime.NeuralState,
                            sustainedDriveScale);
            }

            _lastStep =
                _runtime.Step(
                    1f);

            completedSteps++;

            if (_motionSweepActive &&
                _motionSweepRecorder is not null)
            {
                _motionSweepRecorder.RecordStep(
                    _runtime.NeuralState,
                    1f,
                    frameStimulus,
                    presentedEdgeP);
            }

            if (applyLiveStimulus &&
                _liveStimulus &&
                GetStimulusMode() ==
                    "moving-edge")
            {
                completedSweep =
                    AdvanceMovingEdge();

                if (completedSweep)
                {
                    break;
                }
            }
        }

        timer.Stop();

        _lastStepWallTimeMs =
            timer.Elapsed.TotalMilliseconds;

        _lastAdvancedSimulationMs =
            completedSteps;

        RefreshViewer();

        if (completedSweep)
        {
            CompleteMotionSweep();
        }
    }

    /// <summary>
    /// Advances the moving edge and reports whether an automated experiment has
    /// reached the opposite visual-field boundary.
    /// </summary>
    private bool AdvanceMovingEdge()
    {
        if (_runtime is null ||
            _stimulusColumn is null)
        {
            return false;
        }

        var speed =
            _motionSweepActive
                ? GetMotionSweepSpeed()
                : GetMotionSpeed();

        _movingEdgeP +=
            GetMotionDirection() *
            speed;

        var bounds =
            GetStimulusHemispherePBounds();

        var outside =
            _movingEdgeP <
                bounds.Minimum ||
            _movingEdgeP >
                bounds.Maximum;

        if (!outside)
        {
            RebuildVisualFrame();

            return false;
        }

        _movingEdgeP =
            Math.Clamp(
                _movingEdgeP,
                bounds.Minimum,
                bounds.Maximum);

        RebuildVisualFrame();

        if (_motionSweepActive)
        {
            return true;
        }

        LiveStimulusToggleButton.IsChecked =
            false;

        _liveStimulus =
            false;

        return false;
    }

    private void RebuildVisualFrame()
    {
        if (_runtime is null ||
            _stimulusColumn is null)
        {
            return;
        }

        _visualFrame =
            GetStimulusMode() switch
            {
                "spot" =>
                    VisualFieldFrameGenerator.CreateSpot(
                        _runtime.VisualColumns,
                        _stimulusColumn,
                        radius: 4.0),

                "bar" =>
                    VisualFieldFrameGenerator.CreateVerticalBar(
                        _runtime.VisualColumns,
                        _stimulusColumn,
                        halfWidth: 2.0),

                "edge" =>
                    VisualFieldFrameGenerator.CreateVerticalEdgeAtP(
                        _runtime.VisualColumns,
                        _stimulusColumn.Hemisphere,
                        _movingEdgeP,
                        GetMovingEdgeBrightGreaterThanEdge()),

                "moving-edge" =>
                    VisualFieldFrameGenerator.CreateVerticalEdgeAtP(
                        _runtime.VisualColumns,
                        _stimulusColumn.Hemisphere,
                        _movingEdgeP,
                        GetMovingEdgeBrightGreaterThanEdge()),

                _ =>
                    VisualFieldFrameGenerator.CreateSingleColumn(
                        _runtime.VisualColumns,
                        _stimulusColumn)
            };

        VisualFieldViewControl.SetFrame(
            _visualFrame);

        if (GetStimulusMode() ==
            "moving-edge")
        {
            StimulusTextBlock.Text =
                $"Moving {GetMotionEdgePolarityName()} edge: " +
                $"{_visualFrame.ActiveColumnCount:N0} illuminated columns. " +
                $"Hemisphere {_stimulusColumn.Hemisphere}, " +
                $"edge P={_movingEdgeP:F2}.";
        }
        else
        {
            StimulusTextBlock.Text =
                $"{GetStimulusModeName()} preview: " +
                $"{_visualFrame.ActiveColumnCount:N0} illuminated columns. " +
                $"Anchor: {_stimulusColumn.Hemisphere} " +
                $"column {_stimulusColumn.ColumnId}, " +
                $"P={_stimulusColumn.P:F1}, " +
                $"Q={_stimulusColumn.Q:F1}.";
        }
    }

    private bool GetMovingEdgeBrightGreaterThanEdge()
    {
        var direction =
            GetMotionDirectionEnum();

        var polarity =
            GetMotionEdgePolarity();

        return polarity switch
        {
            MotionEdgePolarity.On =>
                direction ==
                MotionSweepDirection.PDecreasing,

            MotionEdgePolarity.Off =>
                direction ==
                MotionSweepDirection.PIncreasing,

            _ =>
                true
        };
    }

    private string GetStimulusMode()
    {
        if (StimulusModeComboBox.SelectedItem
            is ComboBoxItem item &&
            item.Tag is string tag)
        {
            return tag;
        }

        return "column";
    }

    private string GetStimulusModeName()
    {
        return GetStimulusMode() switch
        {
            "spot" =>
                "Spot",

            "bar" =>
                "Vertical bar",

            "edge" =>
                "Vertical edge",

            "moving-edge" =>
                "Moving edge",

            _ =>
                "Single column"
        };
    }

    private double GetMotionDirection()
    {
        return GetMotionDirectionEnum() ==
               MotionSweepDirection.PIncreasing
            ? 1
            : -1;
    }

    private MotionSweepDirection GetMotionDirectionEnum()
    {
        if (MotionDirectionComboBox.SelectedItem
            is ComboBoxItem item &&
            item.Tag is string tag &&
            tag ==
            "-1")
        {
            return MotionSweepDirection.PDecreasing;
        }

        return MotionSweepDirection.PIncreasing;
    }

    private double GetMotionSpeed()
    {
        return ReadDoubleTag(
            MotionSpeedComboBox,
            fallback: 0.5);
    }

    private double GetMotionSweepSpeed()
    {
        return ReadDoubleTag(
            MotionSweepSpeedComboBox,
            fallback: 0.5);
    }

    private MotionEdgePolarity GetMotionEdgePolarity()
    {
        if (MotionPolarityComboBox.SelectedItem
            is ComboBoxItem item &&
            item.Tag is string tag &&
            tag.Equals(
                "off",
                StringComparison.OrdinalIgnoreCase))
        {
            return MotionEdgePolarity.Off;
        }

        return MotionEdgePolarity.On;
    }

    private static double ReadDoubleTag(
        ComboBox comboBox,
        double fallback)
    {
        if (comboBox.SelectedItem
            is ComboBoxItem item &&
            item.Tag is string tag &&
            double.TryParse(
                tag,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value))
        {
            return value;
        }

        return fallback;
    }

    private static string GetMotionDirectionName(
        MotionSweepDirection direction)
    {
        return direction ==
               MotionSweepDirection.PIncreasing
            ? "P increasing"
            : "P decreasing";
    }

    private static string GetMotionEdgePolarityName(
        MotionEdgePolarity polarity)
    {
        return polarity ==
               MotionEdgePolarity.On
            ? "ON"
            : "OFF";
    }

    private string GetMotionEdgePolarityName()
    {
        return GetMotionEdgePolarityName(
            GetMotionEdgePolarity());
    }

    private (
        double Minimum,
        double Maximum)
        GetStimulusHemispherePBounds()
    {
        if (_runtime is null ||
            _stimulusColumn is null)
        {
            throw new InvalidOperationException(
                "Visual runtime is not ready.");
        }

        var minimum =
            double.PositiveInfinity;

        var maximum =
            double.NegativeInfinity;

        foreach (var column in
                 _runtime.VisualColumns.Columns)
        {
            if (!column.Hemisphere.Equals(
                    _stimulusColumn.Hemisphere,
                    StringComparison.OrdinalIgnoreCase) ||
                !double.IsFinite(
                    column.P))
            {
                continue;
            }

            minimum =
                Math.Min(
                    minimum,
                    column.P);

            maximum =
                Math.Max(
                    maximum,
                    column.P);
        }

        if (!double.IsFinite(
                minimum) ||
            !double.IsFinite(
                maximum))
        {
            throw new InvalidOperationException(
                "No usable P-coordinate bounds were found.");
        }

        return (
            minimum,
            maximum);
    }

    private void RefreshViewer()
    {
        BrainViewControl.RefreshActivity();

        UpdateActivityDisplay();
        UpdatePathwayDisplay();
        UpdateMotionDisplay();
        UpdateSelectedNeuronDisplay();
        UpdateBehaviourDisplay();

        if (_runtime is null ||
            _lastStep is null)
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
            $"{realTimeFactor:F2}×";
    }

    private void UpdatePathwayDisplay()
    {
        if (_runtime is null ||
            _visualPathwayMonitor is null)
        {
            PathwayListBox.ItemsSource =
                Array.Empty<VisualPathwayListItem>();

            return;
        }

        PathwayListBox.ItemsSource =
            _visualPathwayMonitor
                .Capture(
                    _runtime.NeuralState)
                .Select(
                    activity =>
                        new VisualPathwayListItem(
                            activity))
                .ToArray();
    }

    private void UpdateMotionDisplay()
    {
        if (_runtime is null ||
            _motionSweepRecorder is null)
        {
            MotionSweepListBox.ItemsSource =
                Array.Empty<MotionSweepListItem>();

            return;
        }

        var current =
            _motionSweepRecorder.CaptureCurrent(
                _runtime.NeuralState);

        MotionSweepListBox.ItemsSource =
            current
                .Select(
                    activity =>
                        new MotionSweepListItem(
                            activity,
                            _increasingMotionResult
                                ?.GetSubtype(
                                    activity.Type),
                            _decreasingMotionResult
                                ?.GetSubtype(
                                    activity.Type)))
                .ToArray();

        if (_motionSweepActive)
        {
            return;
        }

        if (_increasingMotionResult is not null &&
            _decreasingMotionResult is not null)
        {
            MotionExperimentTextBlock.Text =
                BuildExposureComparisonText(
                    _increasingMotionResult,
                    _decreasingMotionResult);

            return;
        }

        if (_increasingMotionResult is not null)
        {
            MotionExperimentTextBlock.Text =
                BuildSingleExposureText(
                    _increasingMotionResult);

            return;
        }

        if (_decreasingMotionResult is not null)
        {
            MotionExperimentTextBlock.Text =
                BuildSingleExposureText(
                    _decreasingMotionResult);

            return;
        }

        MotionExperimentTextBlock.Text =
            "Run both directional sweeps to establish a baseline.";
    }

    private static string BuildSingleExposureText(
        MotionSweepResult result)
    {
        var exposure =
            result.Exposure;

        return
            $"{GetMotionDirectionName(result.Direction)} " +
            $"{GetMotionEdgePolarityName(result.Polarity)} sweep recorded. " +
            $"{exposure.FrameCount:N0} frames, " +
            $"{exposure.MeanIlluminatedColumnsPerFrame:F1} mean columns/frame, " +
            $"{exposure.MeanPhotoreceptorsPerFrame:F1} photoreceptors/frame, " +
            $"{exposure.MeanTargetsPerFrame:F1} targets/frame. " +
            $"P {exposure.StartP:F1} → {exposure.EndP:F1}.";
    }

    private static string BuildExposureComparisonText(
        MotionSweepResult increasing,
        MotionSweepResult decreasing)
    {
        var positive =
            increasing.Exposure;

        var negative =
            decreasing.Exposure;

        var columnRatio =
            Ratio(
                positive.TotalIlluminatedColumnSamples,
                negative.TotalIlluminatedColumnSamples);

        var photoreceptorRatio =
            Ratio(
                positive.TotalPhotoreceptorSamples,
                negative.TotalPhotoreceptorSamples);

        var targetRatio =
            Ratio(
                positive.TotalTargetSamples,
                negative.TotalTargetSamples);

        return
            $"Both {GetMotionEdgePolarityName(increasing.Polarity)} sweeps recorded. " +
            $"P+: {positive.FrameCount:N0} frames, " +
            $"{positive.MeanIlluminatedColumnsPerFrame:F1} cols/frame, " +
            $"{positive.MeanPhotoreceptorsPerFrame:F1} photo/frame, " +
            $"{positive.MeanTargetsPerFrame:F1} targets/frame, " +
            $"P {positive.StartP:F1}→{positive.EndP:F1}.  " +
            $"P-: {negative.FrameCount:N0} frames, " +
            $"{negative.MeanIlluminatedColumnsPerFrame:F1} cols/frame, " +
            $"{negative.MeanPhotoreceptorsPerFrame:F1} photo/frame, " +
            $"{negative.MeanTargetsPerFrame:F1} targets/frame, " +
            $"P {negative.StartP:F1}→{negative.EndP:F1}.  " +
            $"P+/P- exposure ratios: columns {columnRatio:F3}, " +
            $"photoreceptors {photoreceptorRatio:F3}, " +
            $"targets {targetRatio:F3}.";
    }

    private static double Ratio(
        long first,
        long second)
    {
        return second == 0
            ? double.NaN
            : first /
              (double)second;
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
            _runtime.IdentityTable.GetName(
                neuronIndex)
            ?? "(unnamed)";

        var type =
            _runtime.IdentityTable.GetPrimaryType(
                neuronIndex)
            ?? "(untyped)";

        var side =
            _runtime.IdentityTable.GetSide(
                neuronIndex)
            ?? "unknown";

        var flow =
            _runtime.IdentityTable.GetFlow(
                neuronIndex)
            ?? "unknown";

        var rootId =
            _runtime.NeuronIndexMap.GetRootId(
                neuronIndex);

        var transmitter =
            _runtime.NeuronTable
                .GetNeurotransmitterType(
                    neuronIndex);

        var membranePotential =
            _runtime.NeuralState
                .GetMembranePotentialMv(
                    neuronIndex);

        var synapticInput =
            _runtime.NeuralState
                .GetSynapticInputMv(
                    neuronIndex);

        var fired =
            _runtime.NeuralState
                .DidFire(
                    neuronIndex);

        var outgoingConnections =
            _runtime.Connectome
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

        if (_selectedNeuronIndex is null)
        {
            return;
        }

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
                "No descending neurons identified.";

            return;
        }

        var activeDescending =
            new List<ActiveNeuronListItem>();

        foreach (var neuronIndex in
                 _descendingNeuronIndices)
        {
            var input =
                _runtime.NeuralState
                    .GetSynapticInputMv(
                        neuronIndex);

            var fired =
                _runtime.NeuralState
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
                "neurons identified; none currently active.\n\n" +
                "Motor interpretation remains deliberately unassigned " +
                "until BANC/VNC connectivity is incorporated.";

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
                _runtime.IdentityTable.GetSide(
                    active.NeuronIndex)
                ?? "?";

            lines.Add(
                $"{active.Name} ({side}) " +
                $"input={active.Input:F2}");
        }

        lines.Add("");
        lines.Add(
            "Semantic motor mapping pending BANC/VNC.");

        BehaviourTextBlock.Text =
            string.Join(
                Environment.NewLine,
                lines);
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
                runtime.NeuralState
                    .GetSynapticInputMv(
                        neuronIndex);

            var fired =
                runtime.NeuralState
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

    private static ActiveNeuronListItem CreateActivityItem(
        FlyDoomRuntime runtime,
        int neuronIndex)
    {
        var name =
            runtime.IdentityTable.GetName(
                neuronIndex)
            ?? "(unnamed)";

        var type =
            runtime.IdentityTable.GetPrimaryType(
                neuronIndex)
            ?? "(untyped)";

        return new ActiveNeuronListItem(
            neuronIndex,
            runtime.NeuralState
                .GetSynapticInputMv(
                    neuronIndex),
            runtime.NeuralState
                .GetMembranePotentialMv(
                    neuronIndex),
            runtime.NeuralState
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
            if (ContainsDescending(
                    runtime.IdentityTable.GetFlow(
                        neuronIndex)) ||
                ContainsDescending(
                    runtime.IdentityTable.GetSuperClass(
                        neuronIndex)) ||
                ContainsDescending(
                    runtime.IdentityTable.GetClass(
                        neuronIndex)))
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
                    StringComparison.OrdinalIgnoreCase) ||
                !double.IsFinite(
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
                $"No usable visual column found for hemisphere " +
                $"'{hemisphere}'.");
    }
}