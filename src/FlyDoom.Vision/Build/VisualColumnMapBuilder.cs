using FlyDoom.Vision.Model;

namespace FlyDoom.Vision.Build;

/// <summary>
/// Groups neuron-level column assignments into spatial visual columns.
/// </summary>
public static class VisualColumnMapBuilder
{
    /// <summary>
    /// Builds spatial visual columns from the compact visual-neuron catalogue.
    /// </summary>
    /// <remarks>
    /// Column type is deliberately not part of the grouping key. Multiple
    /// neuron types occupy the same spatial visual column.
    /// </remarks>
    public static VisualColumnMap Build(
        VisualNeuronCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(
            catalog);

        var groups =
            new Dictionary<string, ColumnAccumulator>(
                StringComparer.OrdinalIgnoreCase);

        for (var neuronIndex = 0;
             neuronIndex < catalog.Count;
             neuronIndex++)
        {
            if (!catalog.HasColumnAssignment(
                    neuronIndex))
            {
                continue;
            }

            var hemisphere =
                catalog.GetColumnHemisphere(
                    neuronIndex);

            var columnId =
                catalog.GetColumnId(
                    neuronIndex);

            if (string.IsNullOrWhiteSpace(
                    hemisphere))
            {
                throw new InvalidDataException(
                    $"Visual column assignment for neuron " +
                    $"{neuronIndex} has no hemisphere.");
            }

            if (string.IsNullOrWhiteSpace(
                    columnId))
            {
                throw new InvalidDataException(
                    $"Visual column assignment for neuron " +
                    $"{neuronIndex} has no column ID.");
            }

            var key =
                $"{hemisphere.Trim()}\u001F" +
                $"{columnId.Trim()}";

            if (!groups.TryGetValue(
                    key,
                    out var accumulator))
            {
                accumulator =
                    new ColumnAccumulator(
                        hemisphere.Trim(),
                        columnId.Trim());

                groups.Add(
                    key,
                    accumulator);
            }

            accumulator.Add(
                neuronIndex,
                catalog.GetColumnX(
                    neuronIndex),
                catalog.GetColumnY(
                    neuronIndex),
                catalog.GetColumnP(
                    neuronIndex),
                catalog.GetColumnQ(
                    neuronIndex));
        }

        var columns =
            groups
                .Values
                .OrderBy(
                    group => group.Hemisphere,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    group => GetNumericColumnId(
                        group.ColumnId))
                .ThenBy(
                    group => group.ColumnId,
                    StringComparer.OrdinalIgnoreCase)
                .Select(
                    group => group.Build())
                .ToArray();

        return new VisualColumnMap(
            columns);
    }

    private static int GetNumericColumnId(
        string columnId)
    {
        return int.TryParse(
            columnId,
            out var value)
            ? value
            : int.MaxValue;
    }

    /// <summary>
    /// Collects neurons and coordinates while a column is being built.
    /// </summary>
    private sealed class ColumnAccumulator
    {
        private readonly List<int> _neuronIndices =
            [];

        private double _xSum;
        private int _xCount;

        private double _ySum;
        private int _yCount;

        private double _pSum;
        private int _pCount;

        private double _qSum;
        private int _qCount;

        public string Hemisphere { get; }

        public string ColumnId { get; }

        public ColumnAccumulator(
            string hemisphere,
            string columnId)
        {
            Hemisphere =
                hemisphere;

            ColumnId =
                columnId;
        }

        public void Add(
            int neuronIndex,
            double x,
            double y,
            double p,
            double q)
        {
            _neuronIndices.Add(
                neuronIndex);

            AddCoordinate(
                x,
                ref _xSum,
                ref _xCount);

            AddCoordinate(
                y,
                ref _ySum,
                ref _yCount);

            AddCoordinate(
                p,
                ref _pSum,
                ref _pCount);

            AddCoordinate(
                q,
                ref _qSum,
                ref _qCount);
        }

        public VisualColumn Build()
        {
            return new VisualColumn(
                Hemisphere,
                ColumnId,
                _neuronIndices.ToArray(),
                MeanOrNaN(
                    _xSum,
                    _xCount),
                MeanOrNaN(
                    _ySum,
                    _yCount),
                MeanOrNaN(
                    _pSum,
                    _pCount),
                MeanOrNaN(
                    _qSum,
                    _qCount));
        }

        private static void AddCoordinate(
            double value,
            ref double sum,
            ref int count)
        {
            if (!double.IsFinite(value))
            {
                return;
            }

            sum += value;
            count++;
        }

        private static double MeanOrNaN(
            double sum,
            int count)
        {
            return count == 0
                ? double.NaN
                : sum / count;
        }
    }
}