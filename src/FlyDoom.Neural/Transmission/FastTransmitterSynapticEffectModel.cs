using FlyDoom.Core.Biology;

namespace FlyDoom.Neural.Transmission;

/// <summary>
/// Estimates fast synaptic effects using predicted neurotransmitter
/// identity and the fraction of a target neuron's anatomical input
/// represented by a connection.
/// </summary>
/// <remarks>
/// This is an initial reference model.
///
/// Acetylcholine is treated as excitatory, GABA as inhibitory, and
/// glutamate as inhibitory for the initial CNS model.
///
/// Glutamatergic signalling in Drosophila can be context-dependent, so
/// this assumption should be replaced where more specific receptor or
/// cell-type evidence is available.
///
/// Monoaminergic transmitters are not assigned a fast voltage effect
/// here. Their modulatory roles will be modelled separately.
/// </remarks>
public sealed class FastTransmitterSynapticEffectModel :
    ISynapticEffectModel
{
    private readonly PostsynapticInputTable _postsynapticInputs;
    private readonly float _fullInputDriveMv;

    /// <summary>
    /// Initialises the fast transmitter effect model.
    /// </summary>
    /// <param name="postsynapticInputs">
    /// Total anatomical input received by each neuron.
    /// </param>
    /// <param name="fullInputDriveMv">
    /// Absolute drive corresponding to activation of 100% of a neuron's
    /// anatomical input. This remains a modelling parameter rather than
    /// a measured universal Drosophila value.
    /// </param>
    public FastTransmitterSynapticEffectModel(
        PostsynapticInputTable postsynapticInputs,
        float fullInputDriveMv)
    {
        ArgumentNullException.ThrowIfNull(
            postsynapticInputs);

        if (fullInputDriveMv <= 0 ||
            !float.IsFinite(fullInputDriveMv))
        {
            throw new ArgumentOutOfRangeException(
                nameof(fullInputDriveMv));
        }

        _postsynapticInputs =
            postsynapticInputs;

        _fullInputDriveMv =
            fullInputDriveMv;
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
               _fullInputDriveMv;
    }

    private static float GetFastTransmissionSign(
        NeurotransmitterType neurotransmitterType)
    {
        return neurotransmitterType switch
        {
            NeurotransmitterType.Acetylcholine => 1f,

            NeurotransmitterType.Gaba => -1f,

            // Frequently inhibitory in the Drosophila CNS, but this is
            // explicitly a provisional assumption rather than a universal
            // property of glutamatergic synapses.
            NeurotransmitterType.Glutamate => -1f,

            NeurotransmitterType.Dopamine => 0f,
            NeurotransmitterType.Serotonin => 0f,
            NeurotransmitterType.Octopamine => 0f,
            NeurotransmitterType.Unknown => 0f,

            _ => 0f
        };
    }
}