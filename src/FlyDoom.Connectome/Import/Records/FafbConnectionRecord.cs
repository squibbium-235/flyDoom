using CsvHelper.Configuration.Attributes;

namespace FlyDoom.Connectome.Import.Records;

/// <summary>
/// Represents one connection entry from the FAFB v783 Princeton connectivity dataset.
/// </summary>
/// <remarks>
/// A connection row represents connectivity between two neurons withing
/// a single neuropil and may represent multiple individual synapses.
/// </remarks>
public sealed class FafbConnectionRecord
{
    /// <summary>
    /// Gets the FlyWire root ID of the presynaptic neuron.
    /// </summary>
    [Name("pre_root_id")]
    public long PresynapticRootId { get; init; }

    /// <summary>
    /// Gets the FlyWire root ID of the postsynaptic neuron.
    /// </summary>
    [Name("post_root_id")]
    public long PostsynapticRootId { get; init; }

    /// <summary>
    /// Gets the neuropil in which the connection occurs.
    /// </summary>
    [Name("neuropil")]
    public string? Neuropil { get; init; }

    /// <summary>
    /// Gets the number of individual synapses represented by this connection entry.
    /// </summary>
    [Name("syn_count")]
    public int SynapseCount { get; init; }

    /// <summary>
    /// Gets the predicted NT type of the presynaptic neuron.
    /// </summary>
    [Name("nt_type")]
    public string? NeurotransmitterType { get; init; }
}