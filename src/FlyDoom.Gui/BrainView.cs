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
/// The current view represents each neuron using the mean of its available
/// FAFB representative coordinates.
///
/// Rendering uses a lightweight perspective projection and automatically
/// reduces anatomical drawing detail while the camera is moving. Neural
/// activity is always rendered at full detail.
/// </remarks>
public sealed class BrainView : Control
{
    private const int ActivityBrushLevels =
        16;

    private const int InteractionBackgroundStride =
        4;

    private const double MinimumZoom =
        0.25;

    private const double MaximumZoom =
        8.0;

    private const double MinimumVisibleBrainPixels =
        80.0;

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

    private double[] _projectedDepths =
        [];

    private bool[] _projectedVisible =
        [];

    //
    // Only neural activity changes when the simulation advances.
    //
    // Keep a cache of active point indices so moving the camera does not
    // require scanning every neuron's state again.
    //

    private readonly List<int> _activePointIndices =
        [];

    //
    // Picking and programmatic focusing need rapid neuron -> rendered point
    // lookup rather than scanning the entire population.
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

        RebuildActivePointCache();

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
                MinimumZoom,
                MaximumZoom);

        _panX =
            0;

        _panY =
            0;

        if (Bounds.Width > 0 &&
            Bounds.Height > 0)
        {
            var viewportCentreX =
                Bounds.Width /
                2.0;

            var viewportCentreY =
                Bounds.Height /
                2.0;

            var scale =
                Math.Min(
                    Bounds.Width,
                    Bounds.Height) *
                0.92 *
                _zoom;

            var camera =
                CreateCameraTransform(
                    viewportCentreX,
                    viewportCentreY,
                    scale);

            var projected =
                ProjectPoint(
                    _points[pointIndex],
                    camera,
                    clipToViewport: false);

            _panX =
                viewportCentreX -
                projected.ScreenPoint.X;

            _panY =
                viewportCentreY -
                projected.ScreenPoint.Y;

            ClampPan();
        }

        InvalidateVisual();
    }

    /// <summary>
    /// Restores the default camera orientation, zoom and pan.
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
    /// Refreshes cached neural activity after the simulation advances.
    /// </summary>
    public void RefreshActivity()
    {
        RebuildActivePointCache();

        InvalidateVisual();
    }

    public override void Render(
        DrawingContext context)
    {
        base.Render(
            context);

        //
        // Make the entire control participate in hit testing so camera
        // interaction works even over empty parts of the viewport.
        //

        context.DrawRectangle(
            Brushes.Transparent,
            null,
            new Rect(
                0,
                0,
                Bounds.Width,
                Bounds.Height));

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

        var camera =
            CreateCameraTransform(
                centreX,
                centreY,
                baseScale);

        //
        // Project every neuron so picking and activity positions remain exact.
        //

        for (var pointIndex = 0;
             pointIndex < _points.Length;
             pointIndex++)
        {
            var projected =
                ProjectPoint(
                    _points[pointIndex],
                    camera,
                    clipToViewport: true);

            _projectedPoints[
                pointIndex] =
                projected.ScreenPoint;

            _projectedDepths[
                pointIndex] =
                projected.Depth;

            _projectedVisible[
                pointIndex] =
                projected.Visible;
        }

        //
        // Reduce inactive anatomical detail while moving the camera.
        //

        var backgroundStride =
            _rotating ||
            _panning
                ? InteractionBackgroundStride
                : 1;

        for (var pointIndex = 0;
             pointIndex < _points.Length;
             pointIndex += backgroundStride)
        {
            if (!_projectedVisible[
                    pointIndex])
            {
                continue;
            }

            var depth =
                _projectedDepths[
                    pointIndex];

            var depthLevel =
                Math.Clamp(
                    (int)(
                        depth *
                        (InactiveBrushes.Length - 1)),
                    0,
                    InactiveBrushes.Length - 1);

            var screenPoint =
                _projectedPoints[
                    pointIndex];

            var size =
                0.8 +
                depth *
                0.4;

            context.DrawRectangle(
                InactiveBrushes[
                    depthLevel],
                null,
                new Rect(
                    screenPoint.X -
                    size / 2.0,
                    screenPoint.Y -
                    size / 2.0,
                    size,
                    size));
        }

        //
        // Neural activity remains full-detail even while moving the camera.
        //

        foreach (var pointIndex in
                 _activePointIndices)
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
        // Draw the selected neuron last.
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

    private CameraTransform CreateCameraTransform(
        double centreX,
        double centreY,
        double scale)
    {
        return new CameraTransform(
            CosYaw:
                Math.Cos(
                    _yaw),

            SinYaw:
                Math.Sin(
                    _yaw),

            CosPitch:
                Math.Cos(
                    _pitch),

            SinPitch:
                Math.Sin(
                    _pitch),

            CentreX:
                centreX,

            CentreY:
                centreY,

            Scale:
                scale,

            ViewportWidth:
                Bounds.Width,

            ViewportHeight:
                Bounds.Height);
    }

    private static ProjectedBrainPoint ProjectPoint(
        BrainPoint point,
        CameraTransform camera,
        bool clipToViewport)
    {
        var rotatedX =
            camera.CosYaw *
            point.X +
            camera.SinYaw *
            point.Z;

        var yawZ =
            -camera.SinYaw *
            point.X +
            camera.CosYaw *
            point.Z;

        var rotatedY =
            camera.CosPitch *
            point.Y -
            camera.SinPitch *
            yawZ;

        var rotatedZ =
            camera.SinPitch *
            point.Y +
            camera.CosPitch *
            yawZ;

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
                camera.CentreX +
                rotatedX *
                camera.Scale *
                perspective,
                camera.CentreY -
                rotatedY *
                camera.Scale *
                perspective);

        var depth =
            Math.Clamp(
                rotatedZ +
                0.5,
                0,
                1);

        if (!clipToViewport)
        {
            return new ProjectedBrainPoint(
                screenPoint,
                depth,
                true);
        }

        var visible =
            screenPoint.X >= -20 &&
            screenPoint.X <=
                camera.ViewportWidth + 20 &&
            screenPoint.Y >= -20 &&
            screenPoint.Y <=
                camera.ViewportHeight + 20;

        return new ProjectedBrainPoint(
            screenPoint,
            depth,
            visible);
    }

    /// <summary>
    /// Restricts panning so the entire brain cannot leave the viewport.
    /// </summary>
    private void ClampPan()
    {
        if (_points.Length == 0 ||
            Bounds.Width <= 0 ||
            Bounds.Height <= 0)
        {
            return;
        }

        var viewportCentreX =
            Bounds.Width /
            2.0;

        var viewportCentreY =
            Bounds.Height /
            2.0;

        var scale =
            Math.Min(
                Bounds.Width,
                Bounds.Height) *
            0.92 *
            _zoom;

        var camera =
            CreateCameraTransform(
                viewportCentreX,
                viewportCentreY,
                scale);

        var minimumX =
            double.PositiveInfinity;

        var maximumX =
            double.NegativeInfinity;

        var minimumY =
            double.PositiveInfinity;

        var maximumY =
            double.NegativeInfinity;

        foreach (var point in
                 _points)
        {
            var projected =
                ProjectPoint(
                    point,
                    camera,
                    clipToViewport: false);

            if (!projected.Visible)
            {
                continue;
            }

            minimumX =
                Math.Min(
                    minimumX,
                    projected.ScreenPoint.X);

            maximumX =
                Math.Max(
                    maximumX,
                    projected.ScreenPoint.X);

            minimumY =
                Math.Min(
                    minimumY,
                    projected.ScreenPoint.Y);

            maximumY =
                Math.Max(
                    maximumY,
                    projected.ScreenPoint.Y);
        }

        if (!double.IsFinite(
                minimumX) ||
            !double.IsFinite(
                maximumX) ||
            !double.IsFinite(
                minimumY) ||
            !double.IsFinite(
                maximumY))
        {
            return;
        }

        var requiredVisibleX =
            Math.Min(
                MinimumVisibleBrainPixels,
                Bounds.Width /
                3.0);

        var requiredVisibleY =
            Math.Min(
                MinimumVisibleBrainPixels,
                Bounds.Height /
                3.0);

        var minimumPanX =
            requiredVisibleX -
            maximumX;

        var maximumPanX =
            Bounds.Width -
            requiredVisibleX -
            minimumX;

        var minimumPanY =
            requiredVisibleY -
            maximumY;

        var maximumPanY =
            Bounds.Height -
            requiredVisibleY -
            minimumY;

        if (minimumPanX >
            maximumPanX)
        {
            var midpoint =
                (minimumPanX +
                 maximumPanX) /
                2.0;

            minimumPanX =
                midpoint;

            maximumPanX =
                midpoint;
        }

        if (minimumPanY >
            maximumPanY)
        {
            var midpoint =
                (minimumPanY +
                 maximumPanY) /
                2.0;

            minimumPanY =
                midpoint;

            maximumPanY =
                midpoint;
        }

        _panX =
            Math.Clamp(
                _panX,
                minimumPanX,
                maximumPanX);

        _panY =
            Math.Clamp(
                _panY,
                minimumPanY,
                maximumPanY);
    }

    private void RebuildActivePointCache()
    {
        _activePointIndices.Clear();

        if (_runtime is null)
        {
            return;
        }

        for (var pointIndex = 0;
             pointIndex < _points.Length;
             pointIndex++)
        {
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

            if (fired ||
                MathF.Abs(input) >
                0.0001f)
            {
                _activePointIndices.Add(
                    pointIndex);
            }
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

            _projectedDepths =
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

                if (!double.IsFinite(
                        position.X) ||
                    !double.IsFinite(
                        position.Y) ||
                    !double.IsFinite(
                        position.Z))
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
             pointIndex <
             rawPoints.Count;
             pointIndex++)
        {
            var raw =
                rawPoints[
                    pointIndex];

            _points[
                pointIndex] =
                new BrainPoint(
                    raw.NeuronIndex,
                    (raw.X -
                     centreX) /
                    largestRange,
                    (raw.Y -
                     centreY) /
                    largestRange,
                    (raw.Z -
                     centreZ) /
                    largestRange);

            _pointIndexByNeuron[
                raw.NeuronIndex] =
                pointIndex;
        }

        _projectedPoints =
            new Point[
                _points.Length];

        _projectedDepths =
            new double[
                _points.Length];

        _projectedVisible =
            new bool[
                _points.Length];
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

        if (point.Properties
            .IsLeftButtonPressed)
        {
            _rotating =
                true;

            e.Pointer.Capture(
                this);

            e.Handled =
                true;
        }
        else if (point.Properties
            .IsRightButtonPressed)
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

        if (deltaX == 0 &&
            deltaY == 0)
        {
            return;
        }

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

            ClampPan();
        }

        if (_panning)
        {
            _panX +=
                deltaX;

            _panY +=
                deltaY;

            ClampPan();
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

        ClampPan();

        InvalidateVisual();

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
        if (Bounds.Width <= 0 ||
            Bounds.Height <= 0)
        {
            return;
        }

        var pointerPosition =
            e.GetPosition(
                this);

        var oldZoom =
            _zoom;

        _zoom *=
            Math.Pow(
                1.12,
                e.Delta.Y);

        _zoom =
            Math.Clamp(
                _zoom,
                MinimumZoom,
                MaximumZoom);

        //
        // Nothing changed because we have reached a zoom limit.
        //

        if (Math.Abs(
                _zoom -
                oldZoom) <
            0.000001)
        {
            e.Handled =
                true;

            return;
        }

        var zoomRatio =
            _zoom /
            oldZoom;

        var viewportCentreX =
            Bounds.Width /
            2.0;

        var viewportCentreY =
            Bounds.Height /
            2.0;

        //
        // Zoom around the pointer rather than around the centre of the
        // viewport.
        //
        // Before changing zoom:
        //
        //     pointer = centre + pan + projectedPoint * oldScale
        //
        // After changing zoom we alter pan so that same projected point still
        // lands under the cursor:
        //
        //     newPan =
        //         pointer - centre -
        //         (pointer - centre - oldPan) * zoomRatio
        //
        // This gives the familiar map/CAD-style zoom behaviour where the
        // object beneath the cursor remains anchored beneath it.
        //

        _panX =
            pointerPosition.X -
            viewportCentreX -
            (
                pointerPosition.X -
                viewportCentreX -
                _panX
            ) *
            zoomRatio;

        _panY =
            pointerPosition.Y -
            viewportCentreY -
            (
                pointerPosition.Y -
                viewportCentreY -
                _panY
            ) *
            zoomRatio;

        ClampPan();

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

    private readonly record struct CameraTransform(
        double CosYaw,
        double SinYaw,
        double CosPitch,
        double SinPitch,
        double CentreX,
        double CentreY,
        double Scale,
        double ViewportWidth,
        double ViewportHeight);

    private readonly record struct ProjectedBrainPoint(
        Point ScreenPoint,
        double Depth,
        bool Visible);
}