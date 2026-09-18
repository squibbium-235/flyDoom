using FlyDoom.Connectome.Model;
using FlyDoom.Neural.Simulation;
using FlyDoom.Neural.Transmission;
using FlyDoom.Vision.Model;

namespace FlyDoom.Vision.Transmission;

/// <summary>
/// Propagates evidence-supported graded transmission through the early visual
/// system.
/// </summary>
/// <remarks>
/// Only connection pairs with an explicit rule are propagated.
///
/// The structural connectome tells us that two neurons are connected, but does
/// not by itself tell us receptor expression or functional synaptic sign.
/// Unsupported graded connections are therefore deliberately ignored rather
/// than assigned invented physiology.
/// </remarks>
public sealed class GradedVisualTransmissionModel
{
    private const float MinimumMembraneDeviationMv =
        0.05f;

    private readonly int[] _presynapticIndices;

    private readonly int[] _outgoingOffsets;

    private readonly int[] _postsynapticIndices;

    private readonly float[] _structuralFractions;

    private readonly float[] _polarities;

    private readonly int[] _targetVisitGeneration;

    private readonly float _restingPotentialMv;

    private readonly float _referenceVoltageRangeMv;

    private readonly float _fullEffectAmplitudeMv;

    private readonly float _synapticTimeConstantMs;

    private int _visitGeneration;

    /// <summary>
    /// Gets the number of neurons with at least one supported graded output
    /// connection.
    /// </summary>
    public int GradedNeuronCount =>
        _presynapticIndices.Length;

    /// <summary>
    /// Gets the number of structural connections for which the model has an
    /// explicit graded functional rule.
    /// </summary>
    public int SupportedConnectionCount =>
        _postsynapticIndices.Length;

    /// <summary>
    /// Gets the number of biological connection rules currently represented.
    /// </summary>
    public int RuleCount =>
        VisualGradedConnectionRules.All.Count;

    /// <summary>
    /// Creates the evidence-scoped graded transmission model.
    /// </summary>
    public GradedVisualTransmissionModel(
        VisualNeuronCatalog visualCatalog,
        CompactConnectome connectome,
        PostsynapticInputTable postsynapticInputs,
        float restingPotentialMv,
        float referenceVoltageRangeMv,
        float fullEffectAmplitudeMv,
        float synapticTimeConstantMs)
    {
        ArgumentNullException.ThrowIfNull(
            visualCatalog);

        ArgumentNullException.ThrowIfNull(
            connectome);

        ArgumentNullException.ThrowIfNull(
            postsynapticInputs);

        if (visualCatalog.Count !=
                connectome.NeuronCount ||
            postsynapticInputs.Count !=
                connectome.NeuronCount)
        {
            throw new ArgumentException(
                "Visual, connectome and postsynaptic tables must describe " +
                "the same neuron population.");
        }

        if (!float.IsFinite(
                restingPotentialMv))
        {
            throw new ArgumentOutOfRangeException(
                nameof(restingPotentialMv));
        }

        if (!float.IsFinite(
                referenceVoltageRangeMv) ||
            referenceVoltageRangeMv <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(referenceVoltageRangeMv));
        }

        if (!float.IsFinite(
                fullEffectAmplitudeMv) ||
            fullEffectAmplitudeMv <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(fullEffectAmplitudeMv));
        }

        if (!float.IsFinite(
                synapticTimeConstantMs) ||
            synapticTimeConstantMs <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(synapticTimeConstantMs));
        }

        _restingPotentialMv =
            restingPotentialMv;

        _referenceVoltageRangeMv =
            referenceVoltageRangeMv;

        _fullEffectAmplitudeMv =
            fullEffectAmplitudeMv;

        _synapticTimeConstantMs =
            synapticTimeConstantMs;

        //
        // Build a nested case-insensitive lookup:
        //
        //     presynaptic type
        //          -> postsynaptic type
        //              -> functional polarity
        //

        var ruleLookup =
            new Dictionary<
                string,
                Dictionary<string, float>>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var rule in
                 VisualGradedConnectionRules.All)
        {
            if (!ruleLookup.TryGetValue(
                    rule.PresynapticType,
                    out var targets))
            {
                targets =
                    new Dictionary<string, float>(
                        StringComparer.OrdinalIgnoreCase);

                ruleLookup.Add(
                    rule.PresynapticType,
                    targets);
            }

            targets.Add(
                rule.PostsynapticType,
                rule.Polarity);
        }

        var presynapticIndices =
            new List<int>();

        var outgoingOffsets =
            new List<int>
            {
                0
            };

        var postsynapticIndices =
            new List<int>();

        var structuralFractions =
            new List<float>();

        var polarities =
            new List<float>();

        //
        // Convert the relevant subset of the connectome into a small
        // precomputed route table.
        //
        // Runtime propagation therefore needs no cell-type lookup and never
        // touches unsupported graded connections.
        //

        for (var presynapticIndex = 0;
             presynapticIndex < visualCatalog.Count;
             presynapticIndex++)
        {
            var presynapticType =
                visualCatalog.GetType(
                    presynapticIndex);

            if (presynapticType is null ||
                !ruleLookup.TryGetValue(
                    presynapticType,
                    out var allowedTargets))
            {
                continue;
            }

            var routeStart =
                postsynapticIndices.Count;

            var targets =
                connectome.GetPostsynapticIndices(
                    presynapticIndex);

            var synapseCounts =
                connectome.GetSynapseCounts(
                    presynapticIndex);

            for (var connectionIndex = 0;
                 connectionIndex < targets.Length;
                 connectionIndex++)
            {
                var postsynapticIndex =
                    targets[
                        connectionIndex];

                var postsynapticType =
                    visualCatalog.GetType(
                        postsynapticIndex);

                if (postsynapticType is null ||
                    !allowedTargets.TryGetValue(
                        postsynapticType,
                        out var polarity))
                {
                    continue;
                }

                var totalIncomingSynapses =
                    postsynapticInputs
                        .GetTotalIncomingSynapses(
                            postsynapticIndex);

                if (totalIncomingSynapses <= 0)
                {
                    continue;
                }

                postsynapticIndices.Add(
                    postsynapticIndex);

                structuralFractions.Add(
                    synapseCounts[
                        connectionIndex] /
                    (float)totalIncomingSynapses);

                polarities.Add(
                    polarity);
            }

            if (postsynapticIndices.Count ==
                routeStart)
            {
                continue;
            }

            presynapticIndices.Add(
                presynapticIndex);

            outgoingOffsets.Add(
                postsynapticIndices.Count);
        }

        _presynapticIndices =
            presynapticIndices.ToArray();

        _outgoingOffsets =
            outgoingOffsets.ToArray();

        _postsynapticIndices =
            postsynapticIndices.ToArray();

        _structuralFractions =
            structuralFractions.ToArray();

        _polarities =
            polarities.ToArray();

        _targetVisitGeneration =
            new int[
                connectome.NeuronCount];
    }

    /// <summary>
    /// Propagates graded output for one biological timestep.
    /// </summary>
    public GradedTransmissionStatistics Propagate(
        NeuronStateTable state,
        float timestepMs)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        if (_targetVisitGeneration.Length !=
            state.Count)
        {
            throw new ArgumentException(
                "Neural state and graded transmission neuron counts " +
                "do not match.",
                nameof(state));
        }

        if (!float.IsFinite(
                timestepMs) ||
            timestepMs <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timestepMs));
        }

        //
        // Match the continuously generated drive to the existing exponential
        // synaptic decay.
        //

        var driveScale =
            1f -
            MathF.Exp(
                -timestepMs /
                _synapticTimeConstantMs);

        BeginTargetGeneration();

        var activePresynapticCount =
            0;

        long propagatedConnectionCount =
            0;

        var uniqueTargetCount =
            0;

        var maximumAbsoluteEffectMv =
            0f;

        for (var sourcePosition = 0;
             sourcePosition < _presynapticIndices.Length;
             sourcePosition++)
        {
            var presynapticIndex =
                _presynapticIndices[
                    sourcePosition];

            var membraneDeviation =
                state.GetMembranePotentialMv(
                    presynapticIndex) -
                _restingPotentialMv;

            if (MathF.Abs(
                    membraneDeviation) <
                MinimumMembraneDeviationMv)
            {
                continue;
            }

            activePresynapticCount++;

            //
            // Convert membrane deviation into a bounded graded output.
            //

            var normalisedOutput =
                Math.Clamp(
                    membraneDeviation /
                    _referenceVoltageRangeMv,
                    -1f,
                    1f);

            var connectionStart =
                _outgoingOffsets[
                    sourcePosition];

            var connectionEnd =
                _outgoingOffsets[
                    sourcePosition + 1];

            for (var routeIndex = connectionStart;
                 routeIndex < connectionEnd;
                 routeIndex++)
            {
                var postsynapticIndex =
                    _postsynapticIndices[
                        routeIndex];

                var effectMv =
                    normalisedOutput *
                    _polarities[
                        routeIndex] *
                    _structuralFractions[
                        routeIndex] *
                    _fullEffectAmplitudeMv *
                    driveScale;

                if (MathF.Abs(
                        effectMv) <
                    0.000001f)
                {
                    continue;
                }

                state.AddSynapticInputMv(
                    postsynapticIndex,
                    effectMv);

                propagatedConnectionCount++;

                maximumAbsoluteEffectMv =
                    MathF.Max(
                        maximumAbsoluteEffectMv,
                        MathF.Abs(
                            effectMv));

                if (_targetVisitGeneration[
                        postsynapticIndex] ==
                    _visitGeneration)
                {
                    continue;
                }

                _targetVisitGeneration[
                    postsynapticIndex] =
                    _visitGeneration;

                uniqueTargetCount++;
            }
        }

        return new GradedTransmissionStatistics(
            activePresynapticCount,
            propagatedConnectionCount,
            uniqueTargetCount,
            maximumAbsoluteEffectMv);
    }

    private void BeginTargetGeneration()
    {
        if (_visitGeneration ==
            int.MaxValue)
        {
            Array.Clear(
                _targetVisitGeneration);

            _visitGeneration =
                1;

            return;
        }

        _visitGeneration++;
    }
}

/// <summary>
/// Describes graded visual transmission performed during one timestep.
/// </summary>
public readonly record struct GradedTransmissionStatistics(
    int ActivePresynapticNeuronCount,
    long PropagatedConnectionCount,
    int UniqueTargetCount,
    float MaximumAbsoluteEffectMv);