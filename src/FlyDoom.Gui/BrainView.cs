using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FlyDoom.Runtime;

namespace FlyDoom.Gui;

/// <summary>
/// Draws the FAFB brain as an interactive three-dimensional neuron point cloud.
/// </summary>
/// <remarks>
/// FAFB provides representative X/Y/Z coordinates rather than full skeletons
/// in the currently loaded coordinate dataset. Each neuron is therefore shown
/// at the mean of its available representative positions.
///
/// The three-dimensional coordinates are projected onto the two-dimensional
/// Avalonia drawing surface using a lightweight perspective transform.
/// </remarks>
public sealed class BrainView : Control
{
    private const int ActivityBrushLevels =
        16;

    private static readonly IBrush[] InactiveBrushes =
        CreateInactiveBrushes();

    private static readonly IBrush[] ExcitatoryBrushes =
        CreateActivityBrushes(
            red: 255,
            green: 165,
            blue: 60);

    private static readonly IBrush[] InhibitoryBrushes =
        CreateActivityBrushes(
            red: 30,
            green: 200,
            blue: 255);

    private static readonly IBrush FiredBrush =
        new SolidColorBrush(
            Color.FromRgb(
                255,
                245,
                165));

    private static readonly IBrush SelectedBrush =
        new SolidColorBrush(
            Color.FromRgb(
                255,
                80,
                210));

    private static readonly Pen SelectedPen =
        new(
            SelectedBrush,
            1.8);

    private FlyDoomRuntime? _runtime;

    private BrainPoint[] _points =
        [];

    private Point[] _projectedPoints =
        [];

    private bool[] _projectedVisible =
        [];

    //
    // Picking and programmatic focusing happen frequently once the activity
    // viewer is interactive. Keep a direct neuron -> point lookup instead of
    // scanning all 139,255 neurons every time somebody clicks something.
    //

    private readonly Dictionary<int, int> _pointIndexByNeuron =
        [];

    private int? _selectedNeuronIndex;

    private double _yaw;
    private double _pitch;

    private double _zoom =
        1.0;

    private double _panX;
    private double _panY;

    private bool _rotating;
    private bool _panning;
    private bool _dragged;

    private Point _pressPosition;
    private Point _lastPointerPosition;

    /// <summary>
    /// Raised when the user clicks a neuron in the rendered brain.
    /// </summary>
    public event Action<int>? NeuronSelected;

    public BrainView()
    {
        PointerPressed +=
            BrainView_OnPointerPressed;

        PointerMoved +=
            BrainView_OnPointerMoved;

        PointerReleased +=
            BrainView_OnPointerReleased;

        PointerWheelChanged +=
            BrainView_OnPointerWheelChanged;
    }

    /// <summary>
    /// Supplies the loaded fly runtime to visualise.
    /// </summary>
    public void SetRuntime(
        FlyDoomRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(
            runtime);

        _runtime =
            runtime;

        BuildPointCloud();

        ResetView();
    }

    /// <summary>
    /// Selects a neuron for persistent visual highlighting.
    /// </summary>
    public void SetSelectedNeuron(
        int? neuronIndex)
    {
        _selectedNeuronIndex =
            neuronIndex;

        InvalidateVisual();
    }

    /// <summary>
    /// Centres and zooms the camera onto a neuron.
    /// </summary>
    /// <remarks>
    /// The existing three-dimensional orientation is preserved so focusing a
    /// neuron does not unexpectedly rotate the brain. The view is zoomed to
    /// at least the requested level and then panned so the selected neuron is
    /// projected into the centre of the drawing surface.
    /// </remarks>
    public void FocusNeuron(
        int neuronIndex,
        double minimumZoom = 4.5)
    {
        if (_runtime is null)
        {
            return;
        }

        if (!_pointIndexByNeuron.TryGetValue(
                neuronIndex,
                out var pointIndex))
        {
            return;
        }

        _selectedNeuronIndex =
            neuronIndex;

        _zoom =
            Math.Clamp(
                Math.Max(
                    _zoom,
                    minimumZoom),
                0.25,
                8.0);

        //
        // Calculate where the neuron would appear with no panning, then move
        // the camera by precisely the opposite amount.
        //

        _panX =
            0;

        _panY =
            0;

        if (Bounds.Width > 0 &&
            Bounds.Height > 0)
        {
            var centreX =
                Bounds.Width /
                2.0;

            var centreY =
                Bounds.Height /
                2.0;

            var scale =
                Math.Min(
                    Bounds.Width,
                    Bounds.Height) *
                0.92 *
                _zoom;

            var projected =
                ProjectPoint(
                    _points[pointIndex],
                    centreX,
                    centreY,
                    scale);

            _panX =
                centreX -
                projected.ScreenPoint.X;

            _panY =
                centreY -
                projected.ScreenPoint.Y;
        }

        InvalidateVisual();
    }

    /// <summary>
    /// Restores the default camera orientation.
    /// </summary>
    public void ResetView()
    {
        _yaw =
            0;

        _pitch =
            0;

        _zoom =
            1;

        _panX =
            0;

        _panY =
            0;

        InvalidateVisual();
    }

    /// <summary>
    /// Requests that current neural activity be redrawn.
    /// </summary>
    public void RefreshActivity()
    {
        InvalidateVisual();
    }

    public override void Render(
        DrawingContext context)
    {
        base.Render(
            context);

        if (_runtime is null ||
            _points.Length == 0)
        {
            return;
        }

        var centreX =
            Bounds.Width /
            2.0 +
            _panX;

        var centreY =
            Bounds.Height /
            2.0 +
            _panY;

        var baseScale =
            Math.Min(
                Bounds.Width,
                Bounds.Height) *
            0.92 *
            _zoom;

        //
        // Transform the complete anatomical population into screen space.
        //
        // Store the projected coordinates so mouse picking uses exactly the
        // same geometry the user is currently looking at.
        //

        for (var pointIndex = 0;
             pointIndex < _points.Length;
             pointIndex++)
        {
            var point =
                _points[
                    pointIndex];

            var projected =
                ProjectPoint(
                    point,
                    centreX,
                    centreY,
                    baseScale);

            _projectedPoints[
                pointIndex] =
                projected.ScreenPoint;

            _projectedVisible[
                pointIndex] =
                projected.Visible;

            if (!projected.Visible)
            {
                continue;
            }

            var depthLevel =
                Math.Clamp(
                    (int)(
                        projected.Depth *
                        (InactiveBrushes.Length - 1)),
                    0,
                    InactiveBrushes.Length - 1);

            var radius =
                0.45 +
                projected.Depth *
                0.35;

            context.DrawEllipse(
                InactiveBrushes[
                    depthLevel],
                null,
                projected.ScreenPoint,
                radius,
                radius);
        }

        //
        // Activity is rendered separately above the anatomical cloud.
        //
        // Absolute synaptic input controls brightness and size. Excitatory
        // activity is orange, inhibitory activity cyan, and actual spikes
        // pale yellow.
        //

        for (var pointIndex = 0;
             pointIndex < _points.Length;
             pointIndex++)
        {
            if (!_projectedVisible[
                    pointIndex])
            {
                continue;
            }

            var neuronIndex =
                _points[
                    pointIndex]
                    .NeuronIndex;

            var fired =
                _runtime.NeuralState
                    .DidFire(
                        neuronIndex);

            var input =
                _runtime.NeuralState
                    .GetSynapticInputMv(
                        neuronIndex);

            if (!fired &&
                MathF.Abs(input) <= 0.0001f)
            {
                continue;
            }

            var screenPoint =
                _projectedPoints[
                    pointIndex];

            if (fired)
            {
                context.DrawEllipse(
                    FiredBrush,
                    null,
                    screenPoint,
                    4.2,
                    4.2);

                continue;
            }

            var activityLevel =
                GetActivityLevel(
                    MathF.Abs(input));

            var brush =
                input >= 0
                    ? ExcitatoryBrushes[
                        activityLevel]
                    : InhibitoryBrushes[
                        activityLevel];

            var radius =
                1.4 +
                activityLevel *
                0.18;

            context.DrawEllipse(
                brush,
                null,
                screenPoint,
                radius,
                radius);
        }

        //
        // Draw the selected neuron last so it remains visible regardless of
        // whether it is currently electrically active.
        //

        if (_selectedNeuronIndex is not null &&
            _pointIndexByNeuron.TryGetValue(
                _selectedNeuronIndex.Value,
                out var selectedPointIndex) &&
            _projectedVisible[
                selectedPointIndex])
        {
            context.DrawEllipse(
                null,
                SelectedPen,
                _projectedPoints[
                    selectedPointIndex],
                7,
                7);
        }
    }

    private void BuildPointCloud()
    {
        if (_runtime is null)
        {
            _points =
                [];

            _projectedPoints =
                [];

            _projectedVisible =
                [];

            _pointIndexByNeuron.Clear();

            return;
        }

        var positions =
            _runtime.PositionTable;

        var rawPoints =
            new List<RawBrainPoint>(
                positions.NeuronCount);

        var minimumX =
            double.PositiveInfinity;

        var maximumX =
            double.NegativeInfinity;

        var minimumY =
            double.PositiveInfinity;

        var maximumY =
            double.NegativeInfinity;

        var minimumZ =
            double.PositiveInfinity;

        var maximumZ =
            double.NegativeInfinity;

        for (var neuronIndex = 0;
             neuronIndex < positions.NeuronCount;
             neuronIndex++)
        {
            var positionCount =
                positions.GetPositionCount(
                    neuronIndex);

            if (positionCount == 0)
            {
                continue;
            }

            //
            // Multiple representative coordinates are averaged so the
            // overview has one stable point per neuron.
            //

            var xSum =
                0d;

            var ySum =
                0d;

            var zSum =
                0d;

            var validCount =
                0;

            for (var positionIndex = 0;
                 positionIndex < positionCount;
                 positionIndex++)
            {
                var position =
                    positions.GetPosition(
                        neuronIndex,
                        positionIndex);

                if (!double.IsFinite(position.X) ||
                    !double.IsFinite(position.Y) ||
                    !double.IsFinite(position.Z))
                {
                    continue;
                }

                xSum +=
                    position.X;

                ySum +=
                    position.Y;

                zSum +=
                    position.Z;

                validCount++;
            }

            if (validCount == 0)
            {
                continue;
            }

            var x =
                xSum /
                validCount;

            var y =
                ySum /
                validCount;

            var z =
                zSum /
                validCount;

            rawPoints.Add(
                new RawBrainPoint(
                    neuronIndex,
                    x,
                    y,
                    z));

            minimumX =
                Math.Min(
                    minimumX,
                    x);

            maximumX =
                Math.Max(
                    maximumX,
                    x);

            minimumY =
                Math.Min(
                    minimumY,
                    y);

            maximumY =
                Math.Max(
                    maximumY,
                    y);

            minimumZ =
                Math.Min(
                    minimumZ,
                    z);

            maximumZ =
                Math.Max(
                    maximumZ,
                    z);
        }

        var centreX =
            (minimumX +
             maximumX) /
            2.0;

        var centreY =
            (minimumY +
             maximumY) /
            2.0;

        var centreZ =
            (minimumZ +
             maximumZ) /
            2.0;

        //
        // One common scale preserves the real proportions between axes.
        //

        var largestRange =
            Math.Max(
                maximumX -
                minimumX,
                Math.Max(
                    maximumY -
                    minimumY,
                    maximumZ -
                    minimumZ));

        if (largestRange <= 0)
        {
            largestRange =
                1;
        }

        _points =
            new BrainPoint[
                rawPoints.Count];

        _pointIndexByNeuron.Clear();

        for (var pointIndex = 0;
             pointIndex < rawPoints.Count;
             pointIndex++)
        {
            var raw =
                rawPoints[
                    pointIndex];

            _points[
                pointIndex] =
                new BrainPoint(
                    raw.NeuronIndex,
                    (raw.X - centreX) /
                    largestRange,
                    (raw.Y - centreY) /
                    largestRange,
                    (raw.Z - centreZ) /
                    largestRange);

            _pointIndexByNeuron[
                raw.NeuronIndex] =
                pointIndex;
        }

        _projectedPoints =
            new Point[
                _points.Length];

        _projectedVisible =
            new bool[
                _points.Length];
    }

    private ProjectedBrainPoint ProjectPoint(
        BrainPoint point,
        double centreX,
        double centreY,
        double scale)
    {
        var cosYaw =
            Math.Cos(
                _yaw);

        var sinYaw =
            Math.Sin(
                _yaw);

        var cosPitch =
            Math.Cos(
                _pitch);

        var sinPitch =
            Math.Sin(
                _pitch);

        //
        // Rotate around the vertical Y axis.
        //

        var rotatedX =
            cosYaw *
            point.X +
            sinYaw *
            point.Z;

        var yawZ =
            -sinYaw *
            point.X +
            cosYaw *
            point.Z;

        //
        // Rotate around the horizontal X axis.
        //

        var rotatedY =
            cosPitch *
            point.Y -
            sinPitch *
            yawZ;

        var rotatedZ =
            sinPitch *
            point.Y +
            cosPitch *
            yawZ;

        //
        // Lightweight perspective projection.
        //

        const double cameraDistance =
            2.25;

        var cameraDepth =
            cameraDistance -
            rotatedZ;

        if (cameraDepth <= 0.05)
        {
            return new ProjectedBrainPoint(
                default,
                0,
                false);
        }

        var perspective =
            cameraDistance /
            cameraDepth;

        var screenPoint =
            new Point(
                centreX +
                rotatedX *
                scale *
                perspective,
                centreY -
                rotatedY *
                scale *
                perspective);

        var depth =
            Math.Clamp(
                rotatedZ +
                0.5,
                0,
                1);

        var visible =
            screenPoint.X >= -20 &&
            screenPoint.X <= Bounds.Width + 20 &&
            screenPoint.Y >= -20 &&
            screenPoint.Y <= Bounds.Height + 20;

        return new ProjectedBrainPoint(
            screenPoint,
            depth,
            visible);
    }

    private void BrainView_OnPointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        var point =
            e.GetCurrentPoint(
                this);

        _pressPosition =
            point.Position;

        _lastPointerPosition =
            point.Position;

        _dragged =
            false;

        if (point.Properties.IsLeftButtonPressed)
        {
            _rotating =
                true;

            e.Pointer.Capture(
                this);

            e.Handled =
                true;
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            _panning =
                true;

            e.Pointer.Capture(
                this);

            e.Handled =
                true;
        }
    }

    private void BrainView_OnPointerMoved(
        object? sender,
        PointerEventArgs e)
    {
        if (!_rotating &&
            !_panning)
        {
            return;
        }

        var position =
            e.GetPosition(
                this);

        var deltaX =
            position.X -
            _lastPointerPosition.X;

        var deltaY =
            position.Y -
            _lastPointerPosition.Y;

        var totalDeltaX =
            position.X -
            _pressPosition.X;

        var totalDeltaY =
            position.Y -
            _pressPosition.Y;

        if (totalDeltaX *
            totalDeltaX +
            totalDeltaY *
            totalDeltaY >
            9)
        {
            _dragged =
                true;
        }

        if (_rotating)
        {
            _yaw +=
                deltaX *
                0.01;

            _pitch =
                Math.Clamp(
                    _pitch +
                    deltaY *
                    0.01,
                    -1.45,
                    1.45);
        }

        if (_panning)
        {
            _panX +=
                deltaX;

            _panY +=
                deltaY;
        }

        _lastPointerPosition =
            position;

        InvalidateVisual();

        e.Handled =
            true;
    }

    private void BrainView_OnPointerReleased(
        object? sender,
        PointerReleasedEventArgs e)
    {
        var position =
            e.GetPosition(
                this);

        var wasRotating =
            _rotating;

        _rotating =
            false;

        _panning =
            false;

        e.Pointer.Capture(
            null);

        //
        // A short left click selects a neuron. A drag rotates instead.
        //

        if (wasRotating &&
            !_dragged)
        {
            SelectNearestNeuron(
                position);
        }

        e.Handled =
            true;
    }

    private void BrainView_OnPointerWheelChanged(
        object? sender,
        PointerWheelEventArgs e)
    {
        _zoom *=
            Math.Pow(
                1.12,
                e.Delta.Y);

        _zoom =
            Math.Clamp(
                _zoom,
                0.25,
                8.0);

        InvalidateVisual();

        e.Handled =
            true;
    }

    private void SelectNearestNeuron(
        Point position)
    {
        var bestPointIndex =
            -1;

        var bestDistanceSquared =
            10d *
            10d;

        for (var pointIndex = 0;
             pointIndex < _points.Length;
             pointIndex++)
        {
            if (!_projectedVisible[
                    pointIndex])
            {
                continue;
            }

            var screenPoint =
                _projectedPoints[
                    pointIndex];

            var deltaX =
                screenPoint.X -
                position.X;

            var deltaY =
                screenPoint.Y -
                position.Y;

            var distanceSquared =
                deltaX *
                deltaX +
                deltaY *
                deltaY;

            if (distanceSquared >=
                bestDistanceSquared)
            {
                continue;
            }

            bestDistanceSquared =
                distanceSquared;

            bestPointIndex =
                pointIndex;
        }

        if (bestPointIndex < 0)
        {
            return;
        }

        var neuronIndex =
            _points[
                bestPointIndex]
                .NeuronIndex;

        SetSelectedNeuron(
            neuronIndex);

        NeuronSelected?.Invoke(
            neuronIndex);
    }

    private static int GetActivityLevel(
        float absoluteInput)
    {
        //
        // Logarithmic scaling keeps weak activity visible without allowing a
        // handful of exceptionally strong inputs to dominate the display.
        //

        var normalised =
            MathF.Log10(
                1f +
                absoluteInput) /
            MathF.Log10(
                21f);

        return Math.Clamp(
            (int)(
                normalised *
                (ActivityBrushLevels - 1)),
            0,
            ActivityBrushLevels - 1);
    }

    private static IBrush[] CreateInactiveBrushes()
    {
        var brushes =
            new IBrush[
                ActivityBrushLevels];

        for (var level = 0;
             level < brushes.Length;
             level++)
        {
            var alpha =
                (byte)(
                    28 +
                    level *
                    4);

            brushes[level] =
                new SolidColorBrush(
                    Color.FromArgb(
                        alpha,
                        150,
                        165,
                        180));
        }

        return brushes;
    }

    private static IBrush[] CreateActivityBrushes(
        byte red,
        byte green,
        byte blue)
    {
        var brushes =
            new IBrush[
                ActivityBrushLevels];

        for (var level = 0;
             level < brushes.Length;
             level++)
        {
            var alpha =
                (byte)(
                    80 +
                    level *
                    11);

            brushes[level] =
                new SolidColorBrush(
                    Color.FromArgb(
                        alpha,
                        red,
                        green,
                        blue));
        }

        return brushes;
    }

    private readonly record struct RawBrainPoint(
        int NeuronIndex,
        double X,
        double Y,
        double Z);

    private readonly record struct BrainPoint(
        int NeuronIndex,
        double X,
        double Y,
        double Z);

    private readonly record struct ProjectedBrainPoint(
        Point ScreenPoint,
        double Depth,
        bool Visible);
}