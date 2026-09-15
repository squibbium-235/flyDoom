using CsvHelper.Configuration.Attributes;

namespace FlyDoom.Connectome.Import.Records;

/// <summary>
/// Represents one connectivity tag associated with an FAFB neuron.
/// </summary>
public sealed class FafbConnectivityTagRecord
{
    [Name("root_id")]
    public long RootId { get; init; }

    [Name("connectivity_tag")]
    public string? ConnectivityTag { get; init; }
}