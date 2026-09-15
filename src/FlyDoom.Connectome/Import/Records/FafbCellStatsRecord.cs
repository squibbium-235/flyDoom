using CsvHelper.Configuration.Attributes;

namespace FlyDoom.Connectome.Import.Records;

/// <summary>
/// Represents one row from the FAFB v783 cell statistics dataset.
/// </summary>
public sealed class FafbCellStatsRecord
{
    [Name("root_id")]
    public long RootId { get; init; }

    [Name("length_nm")]
    public double? LengthNm { get; init; }

    [Name("area_nm")]
    public double? AreaNm { get; init; }

    [Name("size_nm")]
    public double? SizeNm { get; init; }
}