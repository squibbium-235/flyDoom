using CsvHelper.Configuration.Attributes;

namespace FlyDoom.Connectome.Import.Records;

/// <summary>
/// Represents one row from the FAFB v783 names dataset.
/// </summary>
public sealed class FafbNameRecord
{
    [Name("root_id")]
    public long RootId { get; init; }

    [Name("name")]
    public string? Name { get; init; }

    [Name("group")]
    public string? Group { get; init; }
}