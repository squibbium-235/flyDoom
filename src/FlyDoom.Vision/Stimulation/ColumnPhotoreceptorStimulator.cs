using FlyDoom.Connectome.Model;
using FlyDoom.Neural.Simulation;
using FlyDoom.Neural.Transmission;
using FlyDoom.Vision.Model;

namespace FlyDoom.Vision.Stimulation;

/// <summary>
/// Converts light applied to spatial visual columns into graded
/// photoreceptor output.
/// </summary>
/// <remarks>
/// R1-R6, R7 and R8 photoreceptors are treated as graded sensory neurons.
///
/// R7 and R8 use direct visual-column assignments.
///
/// R1-R6 use conservative cartridge assignments inferred from structural
/// connectivity to column-assigned L1 and L2 neurons.
///
/// The current reference model represents fast photoreceptor histamine as
/// inhibitory postsynaptic drive.
/// </remarks>
public sealed class ColumnPhotoreceptorStimulator
{
    private readonly VisualNeuronCatalog _catalog;

    private readonly CompactConnectome _connectome;

    private readonly PostsynapticInputTable
        _postsynapticInputs;

    private readonly R1R6CartridgeMap?
        _r1R6CartridgeMap;

    private readonly float
        _fullHistamineInputAmplitudeMv;

    /// <summary>
    /// Creates a stimulator using direct R7/R8 assignments only.
    /// </summary>
    public ColumnPhotoreceptorStimulator(
        VisualNeuronCatalog catalog,
        CompactConnectome connectome,
        PostsynapticInputTable postsynapticInputs,
        float fullHistamineInputAmplitudeMv)
    {
        ValidateArguments(
            catalog,
            connectome,
            postsynapticInputs,
            r1R6CartridgeMap: null,
            fullHistamineInputAmplitudeMv);

        _catalog =
            catalog;

        _connectome =
            connectome;

        _postsynapticInputs =
            postsynapticInputs;

        _r1R6CartridgeMap =
            null;

        _fullHistamineInputAmplitudeMv =
            fullHistamineInputAmplitudeMv;
    }

    /// <summary>
    /// Creates a stimulator using direct R7/R8 assignments and inferred
    /// R1-R6 cartridge assignments.
    /// </summary>
    public ColumnPhotoreceptorStimulator(
        VisualNeuronCatalog catalog,
        CompactConnectome connectome,
        PostsynapticInputTable postsynapticInputs,
        R1R6CartridgeMap r1R6CartridgeMap,
        float fullHistamineInputAmplitudeMv)
    {
        ArgumentNullException.ThrowIfNull(
            r1R6CartridgeMap);

        ValidateArguments(
            catalog,
            connectome,
            postsynapticInputs,
            r1R6CartridgeMap,
            fullHistamineInputAmplitudeMv);

        _catalog =
            catalog;

        _connectome =
            connectome;

        _postsynapticInputs =
            postsynapticInputs;

        _r1R6CartridgeMap =
            r1R6CartridgeMap;

        _fullHistamineInputAmplitudeMv =
            fullHistamineInputAmplitudeMv;
    }

    /// <summary>
    /// Applies one full-strength transient flash to one visual column.
    /// </summary>
    public VisualStimulusStatistics ApplyColumnFlash(
        VisualColumn column,
        NeuronStateTable state,
        float intensity)
    {
        ArgumentNullException.ThrowIfNull(
            column);

        ValidateState(
            state);

        ValidateIntensity(
            intensity);

        if (intensity == 0)
        {
            return new VisualStimulusStatistics(
                0,
                0,
                0,
                0);
        }

        var result =
            ApplyStimuli(
                [
                    new VisualColumnStimulus(
                        column,
                        intensity)
                ],
                state,
                driveScale: 1f,
                knownActiveColumnCount: 1);

        return new VisualStimulusStatistics(
            result.PhotoreceptorCount,
            result.AppliedConnectionCount,
            result.UniqueTargetCount,
            result.MostNegativeInputAmplitudeMv);
    }

    /// <summary>
    /// Applies a full-strength transient visual frame.
    /// </summary>
    /// <remarks>
    /// Use this for isolated flashes and one-shot diagnostic stimuli.
    /// </remarks>
    public VisualFrameStimulusStatistics ApplyFrame(
        VisualFieldFrame frame,
        NeuronStateTable state)
    {
        return ApplyFrame(
            frame,
            state,
            driveScale: 1f);
    }

    /// <summary>
    /// Applies a visual frame with a caller-specified per-step drive scale.
    /// </summary>
    /// <remarks>
    /// A scale below one is required when a stimulus is repeatedly presented
    /// at every simulation timestep. Otherwise repeated full-strength impulses
    /// accumulate inside the exponentially decaying synaptic state.
    ///
    /// For a maintained target amplitude with timestep dt and synaptic time
    /// constant tau, use:
    ///
    ///     1 - exp(-dt / tau)
    ///
    /// This makes repeated input converge toward the intended full-strength
    /// synaptic state rather than growing far beyond it.
    /// </remarks>
    public VisualFrameStimulusStatistics ApplyFrame(
        VisualFieldFrame frame,
        NeuronStateTable state,
        float driveScale)
    {
        ArgumentNullException.ThrowIfNull(
            frame);

        ValidateState(
            state);

        ValidateDriveScale(
            driveScale);

        return ApplyStimuli(
            frame.EnumerateActiveColumns(),
            state,
            driveScale,
            frame.ActiveColumnCount);
    }

    private VisualFrameStimulusStatistics ApplyStimuli(
        IEnumerable<VisualColumnStimulus> stimuli,
        NeuronStateTable state,
        float driveScale,
        int? knownActiveColumnCount = null)
    {
        var intensityByPhotoreceptor =
            new Dictionary<int, float>();

        var activeColumnCount =
            0;

        foreach (var stimulus in
                 stimuli)
        {
            if (stimulus.Intensity <= 0)
            {
                continue;
            }

            ValidateIntensity(
                stimulus.Intensity);

            activeColumnCount++;

            foreach (var neuronIndex in
                     GetPhotoreceptorIndices(
                         stimulus.Column))
            {
                if (intensityByPhotoreceptor
                    .TryGetValue(
                        neuronIndex,
                        out var existingIntensity))
                {
                    intensityByPhotoreceptor[
                        neuronIndex] =
                        Math.Max(
                            existingIntensity,
                            stimulus.Intensity);
                }
                else
                {
                    intensityByPhotoreceptor.Add(
                        neuronIndex,
                        stimulus.Intensity);
                }
            }
        }

        long appliedConnectionCount =
            0;

        var inputByTarget =
            new Dictionary<int, float>();

        foreach (var pair in
                 intensityByPhotoreceptor)
        {
            var presynapticIndex =
                pair.Key;

            var intensity =
                pair.Value;

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
                    targets[
                        connectionIndex];

                var totalIncomingSynapses =
                    _postsynapticInputs
                        .GetTotalIncomingSynapses(
                            postsynapticIndex);

                if (totalIncomingSynapses <= 0)
                {
                    continue;
                }

                //
                // Structural synapse count is not itself an electrical weight.
                //
                // For the current reference model, scale by this connection's
                // share of the target's total anatomical input.
                //

                var inputFraction =
                    synapseCounts[
                        connectionIndex] /
                    (float)totalIncomingSynapses;

                var inputAmplitude =
                    -intensity *
                    driveScale *
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
                    inputByTarget[
                        postsynapticIndex] =
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
                : inputByTarget
                    .Values
                    .Min();

        return new VisualFrameStimulusStatistics(
            knownActiveColumnCount ??
            activeColumnCount,
            intensityByPhotoreceptor.Count,
            appliedConnectionCount,
            inputByTarget.Count,
            mostNegativeInput);
    }

    private IEnumerable<int> GetPhotoreceptorIndices(
        VisualColumn column)
    {
        //
        // R7 and R8 have direct medulla-column assignments.
        //

        foreach (var neuronIndex in
                 column.NeuronIndices)
        {
            if (IsInnerPhotoreceptor(
                    _catalog.GetType(
                        neuronIndex)))
            {
                yield return neuronIndex;
            }
        }

        //
        // R1-R6 use reconstructed lamina cartridges.
        //

        if (_r1R6CartridgeMap is null)
        {
            yield break;
        }

        foreach (var neuronIndex in
                 _r1R6CartridgeMap
                     .GetPhotoreceptorIndices(
                         column))
        {
            yield return neuronIndex;
        }
    }

    private void ValidateState(
        NeuronStateTable state)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        if (state.Count !=
            _connectome.NeuronCount)
        {
            throw new ArgumentException(
                "Neural state and connectome neuron counts do not match.",
                nameof(state));
        }
    }

    private static void ValidateIntensity(
        float intensity)
    {
        if (!float.IsFinite(
                intensity) ||
            intensity < 0 ||
            intensity > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intensity));
        }
    }

    private static void ValidateDriveScale(
        float driveScale)
    {
        if (!float.IsFinite(
                driveScale) ||
            driveScale < 0 ||
            driveScale > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(driveScale));
        }
    }

    private static void ValidateArguments(
        VisualNeuronCatalog catalog,
        CompactConnectome connectome,
        PostsynapticInputTable postsynapticInputs,
        R1R6CartridgeMap? r1R6CartridgeMap,
        float fullHistamineInputAmplitudeMv)
    {
        ArgumentNullException.ThrowIfNull(
            catalog);

        ArgumentNullException.ThrowIfNull(
            connectome);

        ArgumentNullException.ThrowIfNull(
            postsynapticInputs);

        if (catalog.Count !=
                connectome.NeuronCount ||
            postsynapticInputs.Count !=
                connectome.NeuronCount)
        {
            throw new ArgumentException(
                "Visual, connectome and postsynaptic tables must " +
                "describe the same neuron population.");
        }

        if (r1R6CartridgeMap is not null &&
            r1R6CartridgeMap.NeuronCount !=
                connectome.NeuronCount)
        {
            throw new ArgumentException(
                "R1-R6 cartridge map and connectome neuron counts " +
                "must match.",
                nameof(r1R6CartridgeMap));
        }

        if (fullHistamineInputAmplitudeMv <= 0 ||
            !float.IsFinite(
                fullHistamineInputAmplitudeMv))
        {
            throw new ArgumentOutOfRangeException(
                nameof(fullHistamineInputAmplitudeMv));
        }
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