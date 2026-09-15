using CsvHelper.Configuration.Attributes;

namespace FlyDoom.Connectome.Import.Records;

/// <summary>
/// Represents one visual-column assignment from FAFB v783.
/// </summary>
public sealed class FafbColumnAssignmentRecord
{
    [Name("root_id")]
    public long RootId { get; init; }

    [Name("hemisphere")]
    public string? Hemisphere { get; init; }

    [Name("type")]
    public string? Type { get; init; }

    [Name("column_id")]
    public string? ColumnId { get; init; }

    [Name("x")]
    public double? X { get; init; }

    [Name("y")]
    public double? Y { get; init; }

    [Name("p")]
    public double? P { get; init; }

    [Name("q")]
    public double? Q { get; init; }
}