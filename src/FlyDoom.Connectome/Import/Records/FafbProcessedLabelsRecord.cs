using CsvHelper.Configuration.Attributes;

namespace FlyDoom.Connectome.Import.Records;

/// <summary>
/// Represents processed annotation labels associated with an FAFB neuron.
/// </summary>
public sealed class FafbProcessedLabelsRecord
{
    [Name("root_id")]
    public long RootId { get; init; }

    [Name("processed_labels")]
    public string? ProcessedLabels { get; init; }
}