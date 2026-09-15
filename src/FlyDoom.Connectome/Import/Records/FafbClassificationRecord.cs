using CsvHelper.Configuration.Attributes;

namespace FlyDoom.Connectome.Import.Records;

/// <summary>
/// Represents one row from the FAFB v783 classification dataset.
/// </summary>
public sealed class FafbClassificationRecord
{
    [Name("root_id")]
    public long RootId { get; init; }

    [Name("flow")]
    public string? Flow { get; init; }

    [Name("super_class")]
    public string? SuperClass { get; init; }

    [Name("class")]
    public string? Class { get; init; }

    [Name("sub_class")]
    public string? SubClass { get; init; }

    [Name("hemilineage")]
    public string? Hemilineage { get; init; }

    [Name("side")]
    public string? Side { get; init; }

    [Name("nerve")]
    public string? Nerve { get; init; }
}