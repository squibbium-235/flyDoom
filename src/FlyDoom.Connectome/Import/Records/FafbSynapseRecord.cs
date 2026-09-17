using CsvHelper.Configuration.Attributes;

namespace FlyDoom.Connectome.Import.Records;

/// <summary>
/// Represents one synapse from the FAFB v783 Princeton synapse dataset.
/// </summary>
public sealed class FafbSynapseRecord
{
    [Name("pre_x")]
    public long PreX { get; init; }

    [Name("pre_y")]
    public long PreY { get; init; }

    [Name("pre_z")]
    public long PreZ { get; init; }

    [Name("ctr_x")]
    public long CentreX { get; init; }

    [Name("ctr_y")]
    public long CentreY { get; init; }

    [Name("ctr_z")]
    public long CentreZ { get; init; }

    [Name("post_x")]
    public long PostX { get; init; }

    [Name("post_y")]
    public long PostY { get; init; }

    [Name("post_z")]
    public long PostZ { get; init; }

    [Name("size")]
    public double? Size { get; init; }

    [Name("pre_root_id_720575940")]
    public long? PresynapticRootId { get; init; }

    [Name("post_root_id_720575940")]
    public long? PostsynapticRootId { get; init; }

    [Name("neuropil")]
    public string? Neuropil { get; init; }
}