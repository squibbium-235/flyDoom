using CsvHelper.Configuration.Attributes;

namespace FlyDoom.Connectome.Import.Records;

/// <summary>
/// Represents one row from the FAFB v783 consolidated cell type dataset
/// </summary>
public sealed class FafbCellTypeRecord
{
    [Name("root_id")]
    public long RootId { get; init; }

    [Name("primary_type")]
    public string? PrimaryType { get; init; }

    [Name("additional_type(s)")]
    public string? AdditionalTypes { get; init; }
}