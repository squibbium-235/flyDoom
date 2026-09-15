using FlyDoom.Core.Biology;

namespace FlyDoom.Neural.Transmission;

/// <summary>
/// Determines the functional effect of an anatomical connection on its
/// postsynaptic neuron.
/// </summary>
/// <remarks>
/// Connectome synapse counts describe anatomical connectivity and are not
/// themselves functional weights. Implementations of this interface define
/// how anatomical evidence is translated into simulated synaptic drive.
/// </remarks>
public interface ISynapticEffectModel
{
    /// <summary>
    /// Calculates the synaptic drive delivered by one aggregated connection.
    /// </summary>
    /// <param name="presynapticIndex">
    /// Compact index of the neuron that fired.
    /// </param>
    /// <param name="postsynapticIndex">
    /// Compact index of the target neuron.
    /// </param>
    /// <param name="synapseCount">
    /// Number of anatomical synapses represented by the connection.
    /// </param>
    /// <param name="neuropilIndex">
    /// Compact index of the neuropil containing the connection.
    /// </param>
    /// <param name="neurotransmitterType">
    /// Predicted neurotransmitter associated with the connection.
    /// </param>
    /// <returns>
    /// Synaptic drive to add to the postsynaptic neuron.
    /// </returns>
    float CalculateDriveMv(
        int presynapticIndex,
        int postsynapticIndex,
        int synapseCount,
        ushort neuropilIndex,
        NeurotransmitterType neurotransmitterType);
}