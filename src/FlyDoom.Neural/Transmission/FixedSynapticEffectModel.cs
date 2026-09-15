using FlyDoom.Core.Biology;

namespace FlyDoom.Neural.Transmission;

/// <summary>
/// Applies a fixed synaptic input amplitude per anatomical synapse.
/// </summary>
/// <remarks>
/// This exists for simulator tests and synthetic networks. It is not a
/// biological model of FAFB connectivity.
/// </remarks>
public sealed class FixedSynapticEffectModel :
    ISynapticEffectModel
{
    private readonly float _inputPerSynapseMv;

    public FixedSynapticEffectModel(
        float inputPerSynapseMv)
    {
        if (!float.IsFinite(inputPerSynapseMv))
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputPerSynapseMv));
        }

        _inputPerSynapseMv =
            inputPerSynapseMv;
    }

    /// <inheritdoc />
    public float CalculateInputAmplitudeMv(
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

        return _inputPerSynapseMv *
               synapseCount;
    }
}