using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FlyDoom.Vision.Model;

namespace FlyDoom.Gui;

/// <summary>
/// Draws the spatial visual columns, current visual stimulus and interactive
/// stimulus anchor.
/// </summary>
public sealed class VisualFieldView : Control
{
    private const double ColumnSelectionRadius =
        22.0;

    private const int StimulusBrushLevels =
        12;

    private static readonly IBrush ColumnBrush =
        new SolidColorBrush(
            Color.FromArgb(
                120,
                105,
                125,
                150));

    private static readonly IBrush[] StimulusBrushes =
        CreateStimulusBrushes();

    private static readonly IBrush SelectedBrush =
        new SolidColorBrush(
            Color.FromRgb(
                255,
                170,
                45));

    private VisualColumnPoint[] _points =
        [];

    private VisualColumn? _stimulatedColumn;

    private VisualFieldFrame? _frame;

    /// <summary>
    /// Raised when a spatial visual column is clicked.
    /// </summary>
    public event Action<VisualColumn>? ColumnSelected;

    public VisualFieldView()
    {
        PointerPressed +=
            VisualFieldView_OnPointerPressed;
    }

    /// <summary>
    /// Loads the visual-column map into the viewer.
    /// </summary>
    public void SetColumns(
        VisualColumnMap columns)
    {
        ArgumentNullException.ThrowIfNull(
            columns);

        BuildPoints(
            columns);

        InvalidateVisual();
    }

    /// <summary>
    /// Sets the visual frame currently being previewed.
    /// </summary>
    public void SetFrame(
        VisualFieldFrame? frame)
    {
        _frame =
            frame;

        InvalidateVisual();
    }

    /// <summary>
    /// Highlights the column used as the stimulus anchor.
    /// </summary>
    public void SetStimulatedColumn(
        VisualColumn? column)
    {
        _stimulatedColumn =
            column;

        InvalidateVisual();
    }

    public override void Render(
        DrawingContext context)
    {
        base.Render(
            context);

        context.DrawRectangle(
            Brushes.Transparent,
            null,
            new Rect(
                0,
                0,
                Bounds.Width,
                Bounds.Height));

        if (_points.Length == 0)
        {
            return;
        }

        const double padding =
            12;

        var width =
            Math.Max(
                1,
                Bounds.Width -
                padding * 2);

        var height =
            Math.Max(
                1,
                Bounds.Height -
                padding * 2);

        for (var pointIndex = 0;
             pointIndex < _points.Length;
             pointIndex++)
        {
            var point =
                _points[
                    pointIndex];

            var intensity =
                GetFrameIntensity(
                    point.Column);

            var selected =
                IsSameColumn(
                    point.Column,
                    _stimulatedColumn);

            IBrush brush;

            double radius;

            if (selected)
            {
                brush =
                    SelectedBrush;

                radius =
                    5.0;
            }
            else if (intensity > 0)
            {
                var level =
                    Math.Clamp(
                        (int)(
                            intensity *
                            (StimulusBrushLevels - 1)),
                        0,
                        StimulusBrushLevels - 1);

                brush =
                    StimulusBrushes[
                        level];

                radius =
                    2.8;
            }
            else
            {
                brush =
                    ColumnBrush;

                radius =
                    1.6;
            }

            var screenPoint =
                new Point(
                    padding +
                    point.X *
                    width,
                    padding +
                    point.Y *
                    height);

            context.DrawEllipse(
                brush,
                null,
                screenPoint,
                radius,
                radius);
        }
    }

    private float GetFrameIntensity(
        VisualColumn column)
    {
        if (_frame is null)
        {
            return 0;
        }

        for (var columnIndex = 0;
             columnIndex <
             _frame.ColumnMap.Columns.Count;
             columnIndex++)
        {
            if (!IsSameColumn(
                    _frame.ColumnMap.Columns[
                        columnIndex],
                    column))
            {
                continue;
            }

            return _frame.GetIntensity(
                columnIndex);
        }

        return 0;
    }

    private void BuildPoints(
        VisualColumnMap columns)
    {
        var output =
            new List<VisualColumnPoint>();

        BuildHemispherePoints(
            columns,
            "left",
            minimumX: 0.02,
            maximumX: 0.47,
            output);

        BuildHemispherePoints(
            columns,
            "right",
            minimumX: 0.53,
            maximumX: 0.98,
            output);

        _points =
            output.ToArray();
    }

    private static void BuildHemispherePoints(
        VisualColumnMap columns,
        string hemisphere,
        double minimumX,
        double maximumX,
        List<VisualColumnPoint> output)
    {
        var hemisphereColumns =
            columns.Columns
                .Where(
                    column =>
                        column.Hemisphere.Equals(
                            hemisphere,
                            StringComparison.OrdinalIgnoreCase) &&
                        double.IsFinite(
                            column.P) &&
                        double.IsFinite(
                            column.Q))
                .ToArray();

        if (hemisphereColumns.Length == 0)
        {
            return;
        }

        var minimumP =
            hemisphereColumns.Min(
                column => column.P);

        var maximumP =
            hemisphereColumns.Max(
                column => column.P);

        var minimumQ =
            hemisphereColumns.Min(
                column => column.Q);

        var maximumQ =
            hemisphereColumns.Max(
                column => column.Q);

        var pRange =
            Math.Max(
                1,
                maximumP -
                minimumP);

        var qRange =
            Math.Max(
                1,
                maximumQ -
                minimumQ);

        foreach (var column in
                 hemisphereColumns)
        {
            var normalisedP =
                (column.P -
                 minimumP) /
                pRange;

            var normalisedQ =
                (column.Q -
                 minimumQ) /
                qRange;

            output.Add(
                new VisualColumnPoint(
                    column,
                    minimumX +
                    normalisedP *
                    (maximumX -
                     minimumX),
                    0.05 +
                    (1.0 -
                     normalisedQ) *
                    0.90));
        }
    }

    private void VisualFieldView_OnPointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        var pointer =
            e.GetCurrentPoint(
                this);

        if (!pointer.Properties
            .IsLeftButtonPressed)
        {
            return;
        }

        var selected =
            FindNearestColumn(
                pointer.Position);

        if (selected is null)
        {
            return;
        }

        SetStimulatedColumn(
            selected);

        ColumnSelected?.Invoke(
            selected);

        e.Handled =
            true;
    }

    private VisualColumn? FindNearestColumn(
        Point position)
    {
        const double padding =
            12;

        var width =
            Math.Max(
                1,
                Bounds.Width -
                padding * 2);

        var height =
            Math.Max(
                1,
                Bounds.Height -
                padding * 2);

        VisualColumn? bestColumn =
            null;

        var bestDistanceSquared =
            ColumnSelectionRadius *
            ColumnSelectionRadius;

        foreach (var point in
                 _points)
        {
            var screenX =
                padding +
                point.X *
                width;

            var screenY =
                padding +
                point.Y *
                height;

            var deltaX =
                screenX -
                position.X;

            var deltaY =
                screenY -
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

            bestColumn =
                point.Column;
        }

        return bestColumn;
    }

    private static bool IsSameColumn(
        VisualColumn first,
        VisualColumn? second)
    {
        if (second is null)
        {
            return false;
        }

        return string.Equals(
                   first.Hemisphere,
                   second.Hemisphere,
                   StringComparison.OrdinalIgnoreCase) &&
               string.Equals(
                   first.ColumnId,
                   second.ColumnId,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static IBrush[] CreateStimulusBrushes()
    {
        var brushes =
            new IBrush[
                StimulusBrushLevels];

        for (var level = 0;
             level < brushes.Length;
             level++)
        {
            var brightness =
                (byte)(
                    100 +
                    level *
                    14);

            brushes[
                level] =
                new SolidColorBrush(
                    Color.FromRgb(
                        brightness,
                        brightness,
                        brightness));
        }

        return brushes;
    }

    private readonly record struct VisualColumnPoint(
        VisualColumn Column,
        double X,
        double Y);
}