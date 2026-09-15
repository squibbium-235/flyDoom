using CsvHelper.Configuration.Attributes;

namespace FlyDoom.Connectome.Import.Records;

/// <summary>
/// Represents visual-system classification metadata for an FAFB neuron.
/// </summary>
public sealed class FafbVisualNeuronTypeRecord
{
    [Name("root_id")]
    public long RootId { get; init; }

    [Name("type")]
    public string? Type { get; init; }

    [Name("family")]
    public string? Family { get; init; }

    [Name("subsystem")]
    public string? Subsystem { get; init; }

    [Name("category")]
    public string? Category { get; init; }

    [Name("side")]
    public string? Side { get; init; }
}