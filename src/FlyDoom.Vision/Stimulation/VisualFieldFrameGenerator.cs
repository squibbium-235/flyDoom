using FlyDoom.Vision.Model;

namespace FlyDoom.Vision.Stimulation;

/// <summary>
/// Creates simple spatial light patterns for testing the visual system.
/// </summary>
/// <remarks>
/// These patterns are diagnostic stimuli defined in FAFB visual-column P/Q
/// coordinates. They are not intended to model the optical properties of the
/// compound eye.
///
/// Their purpose is to provide controlled sensory input before arbitrary image
/// sampling and the DOOM framebuffer are introduced.
/// </remarks>
public static class VisualFieldFrameGenerator
{
    /// <summary>
    /// Illuminates one visual column.
    /// </summary>
    public static VisualFieldFrame CreateSingleColumn(
        VisualColumnMap columns,
        VisualColumn selectedColumn,
        float intensity = 1f)
    {
        ArgumentNullException.ThrowIfNull(
            columns);

        ArgumentNullException.ThrowIfNull(
            selectedColumn);

        ValidateIntensity(
            intensity);

        var intensities =
            new float[
                columns.Columns.Count];

        for (var columnIndex = 0;
             columnIndex < columns.Columns.Count;
             columnIndex++)
        {
            var column =
                columns.Columns[
                    columnIndex];

            if (IsSameColumn(
                    column,
                    selectedColumn))
            {
                intensities[
                    columnIndex] =
                    intensity;
            }
        }

        return new VisualFieldFrame(
            columns,
            intensities);
    }

    /// <summary>
    /// Creates a circular spot around the selected visual location.
    /// </summary>
    public static VisualFieldFrame CreateSpot(
        VisualColumnMap columns,
        VisualColumn centreColumn,
        double radius,
        float intensity = 1f)
    {
        ArgumentNullException.ThrowIfNull(
            columns);

        ArgumentNullException.ThrowIfNull(
            centreColumn);

        if (!double.IsFinite(
                radius) ||
            radius <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius));
        }

        ValidateIntensity(
            intensity);

        var intensities =
            new float[
                columns.Columns.Count];

        var radiusSquared =
            radius *
            radius;

        for (var columnIndex = 0;
             columnIndex < columns.Columns.Count;
             columnIndex++)
        {
            var column =
                columns.Columns[
                    columnIndex];

            if (!IsSameHemisphere(
                    column,
                    centreColumn))
            {
                continue;
            }

            var deltaP =
                column.P -
                centreColumn.P;

            var deltaQ =
                column.Q -
                centreColumn.Q;

            var distanceSquared =
                deltaP *
                deltaP +
                deltaQ *
                deltaQ;

            if (distanceSquared <=
                radiusSquared)
            {
                intensities[
                    columnIndex] =
                    intensity;
            }
        }

        return new VisualFieldFrame(
            columns,
            intensities);
    }

    /// <summary>
    /// Creates a vertical bright bar centred on the selected P coordinate.
    /// </summary>
    public static VisualFieldFrame CreateVerticalBar(
        VisualColumnMap columns,
        VisualColumn centreColumn,
        double halfWidth,
        float intensity = 1f)
    {
        ArgumentNullException.ThrowIfNull(
            columns);

        ArgumentNullException.ThrowIfNull(
            centreColumn);

        if (!double.IsFinite(
                halfWidth) ||
            halfWidth < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(halfWidth));
        }

        ValidateIntensity(
            intensity);

        var intensities =
            new float[
                columns.Columns.Count];

        for (var columnIndex = 0;
             columnIndex < columns.Columns.Count;
             columnIndex++)
        {
            var column =
                columns.Columns[
                    columnIndex];

            if (!IsSameHemisphere(
                    column,
                    centreColumn))
            {
                continue;
            }

            if (Math.Abs(
                    column.P -
                    centreColumn.P) <=
                halfWidth)
            {
                intensities[
                    columnIndex] =
                    intensity;
            }
        }

        return new VisualFieldFrame(
            columns,
            intensities);
    }

    /// <summary>
    /// Creates the original vertical edge through a selected visual column.
    /// </summary>
    public static VisualFieldFrame CreateVerticalEdge(
        VisualColumnMap columns,
        VisualColumn edgeColumn,
        float intensity = 1f)
    {
        ArgumentNullException.ThrowIfNull(
            edgeColumn);

        return CreateVerticalEdgeAtP(
            columns,
            edgeColumn.Hemisphere,
            edgeColumn.P,
            brightGreaterThanEdge: true,
            intensity);
    }

    /// <summary>
    /// Creates a vertical edge at an arbitrary P coordinate.
    /// </summary>
    /// <remarks>
    /// This overload retains the original convention that the region with
    /// P greater than or equal to the edge is illuminated.
    /// </remarks>
    public static VisualFieldFrame CreateVerticalEdgeAtP(
        VisualColumnMap columns,
        string hemisphere,
        double edgeP,
        float intensity = 1f)
    {
        return CreateVerticalEdgeAtP(
            columns,
            hemisphere,
            edgeP,
            brightGreaterThanEdge: true,
            intensity);
    }

    /// <summary>
    /// Creates a vertical edge at an arbitrary P coordinate with explicit
    /// control over which side of the boundary is illuminated.
    /// </summary>
    /// <param name="columns">
    /// Spatial visual-column map.
    /// </param>
    /// <param name="hemisphere">
    /// Eye receiving the stimulus.
    /// </param>
    /// <param name="edgeP">
    /// Position of the edge in visual-column P coordinates.
    /// </param>
    /// <param name="brightGreaterThanEdge">
    /// When true, columns with P greater than or equal to the edge are bright.
    /// When false, columns with P less than or equal to the edge are bright.
    /// </param>
    /// <param name="intensity">
    /// Bright-region intensity from zero to one.
    /// </param>
    public static VisualFieldFrame CreateVerticalEdgeAtP(
        VisualColumnMap columns,
        string hemisphere,
        double edgeP,
        bool brightGreaterThanEdge,
        float intensity = 1f)
    {
        ArgumentNullException.ThrowIfNull(
            columns);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            hemisphere);

        if (!double.IsFinite(
                edgeP))
        {
            throw new ArgumentOutOfRangeException(
                nameof(edgeP));
        }

        ValidateIntensity(
            intensity);

        var intensities =
            new float[
                columns.Columns.Count];

        for (var columnIndex = 0;
             columnIndex < columns.Columns.Count;
             columnIndex++)
        {
            var column =
                columns.Columns[
                    columnIndex];

            if (!column.Hemisphere.Equals(
                    hemisphere,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var illuminated =
                brightGreaterThanEdge
                    ? column.P >= edgeP
                    : column.P <= edgeP;

            if (illuminated)
            {
                intensities[
                    columnIndex] =
                    intensity;
            }
        }

        return new VisualFieldFrame(
            columns,
            intensities);
    }

    private static bool IsSameHemisphere(
        VisualColumn first,
        VisualColumn second)
    {
        return string.Equals(
            first.Hemisphere,
            second.Hemisphere,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameColumn(
        VisualColumn first,
        VisualColumn second)
    {
        return IsSameHemisphere(
                   first,
                   second) &&
               string.Equals(
                   first.ColumnId,
                   second.ColumnId,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateIntensity(
        float intensity)
    {
        if (!float.IsFinite(
                intensity) ||
            intensity < 0 ||
            intensity > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intensity));
        }
    }
}