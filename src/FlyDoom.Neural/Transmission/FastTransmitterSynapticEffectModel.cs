using FlyDoom.Core.Biology;

namespace FlyDoom.Neural.Transmission;

/// <summary>
/// Estimates fast synaptic input using predicted neurotransmitter identity
/// and the fraction of the postsynaptic neuron's anatomical input represented
/// by a connection.
/// </summary>
/// <remarks>
/// This is an initial reference model.
///
/// Acetylcholine is treated as excitatory, GABA as inhibitory, and glutamate
/// as provisionally inhibitory.
///
/// Dopamine, serotonin, and octopamine are not assigned a fast voltage effect
/// here because their modulatory roles will be modelled separately.
///
/// The returned value is an input amplitude for the decaying synaptic state,
/// not a direct change in membrane potential.
/// </remarks>
public sealed class FastTransmitterSynapticEffectModel :
    ISynapticEffectModel
{
    private readonly PostsynapticInputTable _postsynapticInputs;
    private readonly float _fullInputAmplitudeMv;

    public FastTransmitterSynapticEffectModel(
        PostsynapticInputTable postsynapticInputs,
        float fullInputAmplitudeMv)
    {
        ArgumentNullException.ThrowIfNull(
            postsynapticInputs);

        if (fullInputAmplitudeMv <= 0 ||
            !float.IsFinite(fullInputAmplitudeMv))
        {
            throw new ArgumentOutOfRangeException(
                nameof(fullInputAmplitudeMv));
        }

        _postsynapticInputs =
            postsynapticInputs;

        _fullInputAmplitudeMv =
            fullInputAmplitudeMv;
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

        var totalIncomingSynapses =
            _postsynapticInputs.GetTotalIncomingSynapses(
                postsynapticIndex);

        if (totalIncomingSynapses <= 0)
        {
            return 0;
        }

        var sign =
            GetFastTransmissionSign(
                neurotransmitterType);

        if (sign == 0)
        {
            return 0;
        }

        var inputFraction =
            synapseCount /
            (float)totalIncomingSynapses;

        return sign *
               inputFraction *
               _fullInputAmplitudeMv;
    }

    private static float GetFastTransmissionSign(
        NeurotransmitterType neurotransmitterType)
    {
        return neurotransmitterType switch
        {
            NeurotransmitterType.Acetylcholine => 1f,

            NeurotransmitterType.Gaba => -1f,

            NeurotransmitterType.Glutamate => -1f,

            NeurotransmitterType.Dopamine => 0f,
            NeurotransmitterType.Serotonin => 0f,
            NeurotransmitterType.Octopamine => 0f,
            NeurotransmitterType.Unknown => 0f,

            _ => 0f
        };
    }
}