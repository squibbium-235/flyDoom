using FlyDoom.Core.Biology;

namespace FlyDoom.Neural.Transmission;

/// <summary>
/// Determines the functional effect of an anatomical connection on its
/// postsynaptic neuron.
/// </summary>
/// <remarks>
/// Anatomical synapse counts are structural evidence and are not themselves
/// functional synaptic weights.
/// </remarks>
public interface ISynapticEffectModel
{
    /// <summary>
    /// Calculates the amplitude added to the postsynaptic neuron's
    /// decaying synaptic input state.
    /// </summary>
    float CalculateInputAmplitudeMv(
        int presynapticIndex,
        int postsynapticIndex,
        int synapseCount,
        ushort neuropilIndex,
        NeurotransmitterType neurotransmitterType);
}