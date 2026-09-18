namespace FlyDoom.Vision.Model;

/// <summary>
/// Stores the light intensity presented to every spatial visual column in one
/// visual frame.
/// </summary>
/// <remarks>
/// Intensities are normalised to the range zero to one.
///
/// The frame operates in visual-column space rather than screen pixels. This
/// allows synthetic test patterns and future game frames to share the same
/// downstream photoreceptor stimulation path.
/// </remarks>
public sealed class VisualFieldFrame
{
    private readonly float[] _intensities;

    /// <summary>
    /// Gets the visual-column map this frame addresses.
    /// </summary>
    public VisualColumnMap ColumnMap { get; }

    /// <summary>
    /// Gets the number of visual columns represented by this frame.
    /// </summary>
    public int ColumnCount =>
        _intensities.Length;

    /// <summary>
    /// Gets the number of columns receiving non-zero light.
    /// </summary>
    public int ActiveColumnCount { get; }

    internal VisualFieldFrame(
        VisualColumnMap columnMap,
        float[] intensities)
    {
        ArgumentNullException.ThrowIfNull(
            columnMap);

        ArgumentNullException.ThrowIfNull(
            intensities);

        if (intensities.Length !=
            columnMap.Columns.Count)
        {
            throw new ArgumentException(
                "Frame intensity count must match visual column count.",
                nameof(intensities));
        }

        var activeColumnCount =
            0;

        for (var columnIndex = 0;
             columnIndex < intensities.Length;
             columnIndex++)
        {
            var intensity =
                intensities[
                    columnIndex];

            if (!float.IsFinite(
                    intensity) ||
                intensity < 0 ||
                intensity > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(intensities),
                    "Visual intensities must be finite values from zero to one.");
            }

            if (intensity > 0)
            {
                activeColumnCount++;
            }
        }

        ColumnMap =
            columnMap;

        _intensities =
            intensities;

        ActiveColumnCount =
            activeColumnCount;
    }

    /// <summary>
    /// Gets the light intensity applied to one visual column.
    /// </summary>
    public float GetIntensity(
        int columnIndex)
    {
        if ((uint)columnIndex >=
            (uint)_intensities.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columnIndex));
        }

        return _intensities[
            columnIndex];
    }

    /// <summary>
    /// Enumerates only columns receiving non-zero light.
    /// </summary>
    public IEnumerable<VisualColumnStimulus>
        EnumerateActiveColumns()
    {
        for (var columnIndex = 0;
             columnIndex < _intensities.Length;
             columnIndex++)
        {
            var intensity =
                _intensities[
                    columnIndex];

            if (intensity <= 0)
            {
                continue;
            }

            yield return new VisualColumnStimulus(
                ColumnMap.Columns[
                    columnIndex],
                intensity);
        }
    }
}

/// <summary>
/// Associates one spatial visual column with its current light intensity.
/// </summary>
public readonly record struct VisualColumnStimulus(
    VisualColumn Column,
    float Intensity);