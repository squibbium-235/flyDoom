using CsvHelper.Configuration.Attributes;

namespace FlyDoom.Connectome.Import.Records;

/// <summary>
/// Represents one row from the FAFB v783 neurons.csv dataset.
/// </summary>
/// <remarks>
/// This type mirrors the structure of the raw data.
/// It should not be used as the simulated neuron model.
/// </remarks>
public sealed class FafbNeuronRecord
{
    /// <summary>
    /// Gets the unique FlyWire root ID assigned to the neuron.
    /// </summary>
    [Name("root_id")]
    public long RootId { get; init; }

    /// <summary>
    /// Gets the group assigned to the neuron, if one is available.
    /// </summary>
    [Name("group")]
    public string? Group { get; init; }

    /// <summary>
    /// Gets the predicted primary neurotransmitter type.
    /// </summary>
    [Name("nt_type")]
    public string? NeurotransmitterType { get; init; }

    /// <summary>
    /// Gets the confidence score for the predicted neurotransmitter type.
    /// </summary>
    [Name("nt_type_score")]
    public double? NeurotransmitterTypeScore { get; init; }

    /// <summary>
    /// Gets the average predicted dopamine score.
    /// </summary>
    [Name("da_avg")]
    public double? DopamineAverage { get; init; }

    /// <summary>
    /// Gets the average predicted serotonin score.
    /// </summary>
    [Name("ser_avg")]
    public double? SerotoninAverage { get; init; }

    /// <summary>
    /// Gets the average predicted GABA score.
    /// </summary>
    [Name("gaba_avg")]
    public double? GabaAverage { get; init; }

    /// <summary>
    /// Gets the average predicted glutamate score.
    /// </summary>
    [Name("glut_avg")]
    public double? GlutamateAverage { get; init; }

    /// <summary>
    /// Gets the average predicted acetylcholine score.
    /// </summary>
    [Name("ach_avg")]
    public double? AcetylcholineAverage { get; init; }

    /// <summary>
    /// Gets the average predicted octopamine score.
    /// </summary>
    [Name("oct_avg")]
    public double? OctopamineAverage { get; init; }
}