using FlyDoom.Connectome.Model;
using FlyDoom.Neural.Simulation;
using FlyDoom.Neural.Transmission;
using FlyDoom.Vision.Model;

namespace FlyDoom.Vision.Stimulation;

/// <summary>
/// Converts light applied to a spatial visual column into graded
/// photoreceptor output.
/// </summary>
/// <remarks>
/// R7 and R8 photoreceptors are treated as graded, non-spiking sensory
/// neurons. A light flash therefore acts directly through their outgoing
/// anatomical connections rather than forcing the photoreceptors through
/// the reference LIF spike model.
///
/// The current model represents the dominant fast histaminergic effect as
/// inhibitory input to postsynaptic neurons.
///
/// R8 co-transmission and wavelength-specific phototransduction will be
/// introduced separately as the visual model becomes more detailed.
/// </remarks>
public sealed class ColumnPhotoreceptorStimulator
{
    private readonly VisualNeuronCatalog _catalog;
    private readonly CompactConnectome _connectome;
    private readonly PostsynapticInputTable _postsynapticInputs;

    private readonly float _fullHistamineInputAmplitudeMv;

    public ColumnPhotoreceptorStimulator(
        VisualNeuronCatalog catalog,
        CompactConnectome connectome,
        PostsynapticInputTable postsynapticInputs,
        float fullHistamineInputAmplitudeMv)
    {
        ArgumentNullException.ThrowIfNull(
            catalog);

        ArgumentNullException.ThrowIfNull(
            connectome);

        ArgumentNullException.ThrowIfNull(
            postsynapticInputs);

        if (catalog.Count != connectome.NeuronCount ||
            postsynapticInputs.Count != connectome.NeuronCount)
        {
            throw new ArgumentException(
                "Visual, connectome and postsynaptic tables must " +
                "describe the same neuron population.");
        }

        if (fullHistamineInputAmplitudeMv <= 0 ||
            !float.IsFinite(fullHistamineInputAmplitudeMv))
        {
            throw new ArgumentOutOfRangeException(
                nameof(fullHistamineInputAmplitudeMv));
        }

        _catalog =
            catalog;

        _connectome =
            connectome;

        _postsynapticInputs =
            postsynapticInputs;

        _fullHistamineInputAmplitudeMv =
            fullHistamineInputAmplitudeMv;
    }

    /// <summary>
    /// Applies a brief light flash to the R7 and R8 photoreceptors
    /// belonging to one spatial visual column.
    /// </summary>
    /// <param name="column">
    /// Spatial visual column receiving the flash.
    /// </param>
    /// <param name="state">
    /// Neural state receiving the resulting synaptic input.
    /// </param>
    /// <param name="intensity">
    /// Normalised flash intensity from zero to one.
    /// </param>
    public VisualStimulusStatistics ApplyColumnFlash(
        VisualColumn column,
        NeuronStateTable state,
        float intensity)
    {
        ArgumentNullException.ThrowIfNull(
            column);

        ArgumentNullException.ThrowIfNull(
            state);

        if (state.Count !=
            _connectome.NeuronCount)
        {
            throw new ArgumentException(
                "Neural state and connectome neuron counts do not match.",
                nameof(state));
        }

        if (intensity < 0 ||
            intensity > 1 ||
            !float.IsFinite(intensity))
        {
            throw new ArgumentOutOfRangeException(
                nameof(intensity));
        }

        if (intensity == 0)
        {
            return new VisualStimulusStatistics(
                0,
                0,
                0,
                0);
        }

        var photoreceptorCount =
            0;

        long appliedConnectionCount =
            0;

        //
        // Keep track of the total stimulus contributed to each target.
        // Multiple photoreceptors may converge on the same postsynaptic
        // neuron.
        //
        var inputByTarget =
            new Dictionary<int, float>();

        foreach (var presynapticIndex in
                 column.NeuronIndices)
        {
            var visualType =
                _catalog.GetType(
                    presynapticIndex);

            if (!IsInnerPhotoreceptor(
                    visualType))
            {
                continue;
            }

            photoreceptorCount++;

            var targets =
                _connectome.GetPostsynapticIndices(
                    presynapticIndex);

            var synapseCounts =
                _connectome.GetSynapseCounts(
                    presynapticIndex);

            for (var connectionIndex = 0;
                 connectionIndex < targets.Length;
                 connectionIndex++)
            {
                var postsynapticIndex =
                    targets[connectionIndex];

                var totalIncomingSynapses =
                    _postsynapticInputs
                        .GetTotalIncomingSynapses(
                            postsynapticIndex);

                if (totalIncomingSynapses <= 0)
                {
                    continue;
                }

                //
                // Synapse count remains structural evidence rather than a
                // voltage. We scale the photoreceptor's effect by the
                // fraction of the target neuron's anatomical input that
                // this connection represents.
                //
                var inputFraction =
                    synapseCounts[connectionIndex] /
                    (float)totalIncomingSynapses;

                //
                // Histamine is represented as inhibitory fast input.
                //
                var inputAmplitude =
                    -intensity *
                    inputFraction *
                    _fullHistamineInputAmplitudeMv;

                if (inputAmplitude == 0)
                {
                    continue;
                }

                state.AddSynapticInputMv(
                    postsynapticIndex,
                    inputAmplitude);

                appliedConnectionCount++;

                if (inputByTarget.TryGetValue(
                        postsynapticIndex,
                        out var existingInput))
                {
                    inputByTarget[postsynapticIndex] =
                        existingInput +
                        inputAmplitude;
                }
                else
                {
                    inputByTarget.Add(
                        postsynapticIndex,
                        inputAmplitude);
                }
            }
        }

        var mostNegativeInput =
            inputByTarget.Count == 0
                ? 0f
                : inputByTarget.Values.Min();

        return new VisualStimulusStatistics(
            photoreceptorCount,
            appliedConnectionCount,
            inputByTarget.Count,
            mostNegativeInput);
    }

    private static bool IsInnerPhotoreceptor(
        string? visualType)
    {
        return string.Equals(
                   visualType,
                   "R7",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   visualType,
                   "R8",
                   StringComparison.OrdinalIgnoreCase);
    }
}