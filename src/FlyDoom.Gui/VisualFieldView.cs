using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FlyDoom.Vision.Model;

namespace FlyDoom.Gui;

/// <summary>
/// Draws and allows selection of the spatial visual columns of both optic lobes.
/// </summary>
public sealed class VisualFieldView : Control
{
    private static readonly IBrush ColumnBrush =
        new SolidColorBrush(
            Color.FromArgb(
                150,
                125,
                145,
                170));

    private static readonly IBrush StimulatedBrush =
        new SolidColorBrush(
            Color.FromRgb(
                255,
                180,
                55));

    private VisualColumnPoint[] _points =
        [];

    private VisualColumn? _stimulatedColumn;

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
    /// Loads the spatial visual-column map into the viewer.
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
    /// Highlights the visual column that will receive the next stimulus.
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

        foreach (var point in
                 _points)
        {
            var selected =
                ReferenceEquals(
                    point.Column,
                    _stimulatedColumn);

            var brush =
                selected
                    ? StimulatedBrush
                    : ColumnBrush;

            var radius =
                selected
                    ? 5.0
                    : 1.8;

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
            12d *
            12d;

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

    private readonly record struct VisualColumnPoint(
        VisualColumn Column,
        double X,
        double Y);
}