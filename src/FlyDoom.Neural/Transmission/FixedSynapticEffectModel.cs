using FlyDoom.Core.Biology;

namespace FlyDoom.Neural.Transmission;

/// <summary>
/// Applies a fixed amount of synaptic drive per anatomical synapse.
/// </summary>
/// <remarks>
/// This model exists for simulator validation and synthetic networks.
/// It should not be treated as a biological model of FAFB connectivity.
/// </remarks>
public sealed class FixedSynapticEffectModel :
    ISynapticEffectModel
{
    private readonly float _drivePerSynapseMv;

    /// <summary>
    /// Initialises a fixed synaptic effect model.
    /// </summary>
    /// <param name="drivePerSynapseMv">
    /// Drive contributed by each anatomical synapse.
    /// </param>
    public FixedSynapticEffectModel(
        float drivePerSynapseMv)
    {
        if (!float.IsFinite(
                drivePerSynapseMv))
        {
            throw new ArgumentOutOfRangeException(
                nameof(drivePerSynapseMv));
        }

        _drivePerSynapseMv =
            drivePerSynapseMv;
    }

    /// <inheritdoc />
    public float CalculateDriveMv(
        int presynapticIndex,
        int postsynapticIndex,
        int synapseCount,
        ushort neuropilIndex,
        NeurotransmitterType neurotransmitterType)
    {
        if (synapseCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(synapseCount));
        }

        return _drivePerSynapseMv *
               synapseCount;
    }
}