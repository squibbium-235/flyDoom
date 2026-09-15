using CsvHelper.Configuration.Attributes;

namespace FlyDoom.Connectome.Import.Records;

/// <summary>
/// Represents one row from the FAFB v783 coordinate dataset.
/// </summary>
public sealed class FafbCoordinateRecord
{
    [Name("root_id")]
    public long RootId { get; init; }

    /// <summary>
    /// Gets the raw position representation supplied by the dataset.
    /// </summary>
    [Name("position")]
    public string? Position { get; init; }

    /// <summary>
    /// Gets the supervoxel associated with the representative position.
    /// </summary>
    [Name("supervoxel_id")]
    public long? SupervoxelId { get; init; }
}