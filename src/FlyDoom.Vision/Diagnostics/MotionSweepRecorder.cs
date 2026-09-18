using FlyDoom.Neural.Simulation;
using FlyDoom.Vision.Model;
using FlyDoom.Vision.Stimulation;

namespace FlyDoom.Vision.Diagnostics;

/// <summary>
/// Direction used by an automated visual motion sweep.
/// </summary>
public enum MotionSweepDirection
{
    PIncreasing,
    PDecreasing
}

/// <summary>
/// Contrast polarity of a moving visual edge.
/// </summary>
public enum MotionEdgePolarity
{
    On,
    Off
}

/// <summary>
/// Records T4/T5 neural responses and the sensory exposure delivered during a
/// moving-edge experiment.
/// </summary>
/// <remarks>
/// Recording the stimulus itself is important.
///
/// If two direction sweeps receive different numbers of illuminated columns,
/// photoreceptors or downstream target events, a direction-selectivity result
/// may simply reflect unequal sensory exposure rather than neural computation.
/// </remarks>
public sealed class MotionSweepRecorder
{
    private const float ActiveVoltageThresholdMv =
        0.05f;

    private static readonly string[] Types =
    [
        "T4a",
        "T4b",
        "T4c",
        "T4d",
        "T5a",
        "T5b",
        "T5c",
        "T5d"
    ];

    private readonly int[][] _neuronIndicesByType;

    private readonly float[] _peakMeanDeviationMv;

    private readonly double[] _integratedMeanDeviationMvMs;

    private readonly int[] _totalFiredCount;

    private readonly float _restingPotentialMv;

    private MotionSweepDirection _direction;

    private MotionEdgePolarity _polarity;

    private double _durationMs;

    private int _frameCount;

    private long _totalIlluminatedColumnSamples;

    private long _totalPhotoreceptorSamples;

    private long _totalTargetSamples;

    private long _totalAppliedConnectionSamples;

    private double _startP;

    private double _endP;

    /// <summary>
    /// Gets whether a sweep is currently being recorded.
    /// </summary>
    public bool IsRecording
    {
        get;
        private set;
    }

    /// <summary>
    /// Builds the T4/T5 population index used during experiments.
    /// </summary>
    public MotionSweepRecorder(
        VisualNeuronCatalog catalog,
        float restingPotentialMv = -60f)
    {
        ArgumentNullException.ThrowIfNull(
            catalog);

        if (!float.IsFinite(
                restingPotentialMv))
        {
            throw new ArgumentOutOfRangeException(
                nameof(restingPotentialMv));
        }

        _restingPotentialMv =
            restingPotentialMv;

        var typeIndex =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        for (var index = 0;
             index < Types.Length;
             index++)
        {
            typeIndex.Add(
                Types[index],
                index);
        }

        var working =
            new List<int>[
                Types.Length];

        for (var index = 0;
             index < working.Length;
             index++)
        {
            working[index] =
                [];
        }

        for (var neuronIndex = 0;
             neuronIndex < catalog.Count;
             neuronIndex++)
        {
            var type =
                catalog.GetType(
                    neuronIndex);

            if (type is null ||
                !typeIndex.TryGetValue(
                    type,
                    out var index))
            {
                continue;
            }

            working[index].Add(
                neuronIndex);
        }

        _neuronIndicesByType =
            new int[
                Types.Length][];

        for (var index = 0;
             index < Types.Length;
             index++)
        {
            _neuronIndicesByType[index] =
                working[index].ToArray();
        }

        _peakMeanDeviationMv =
            new float[
                Types.Length];

        _integratedMeanDeviationMvMs =
            new double[
                Types.Length];

        _totalFiredCount =
            new int[
                Types.Length];
    }

    /// <summary>
    /// Starts a fresh directional sweep recording.
    /// </summary>
    public void Begin(
        MotionSweepDirection direction,
        MotionEdgePolarity polarity,
        double startP)
    {
        if (!double.IsFinite(
                startP))
        {
            throw new ArgumentOutOfRangeException(
                nameof(startP));
        }

        Array.Clear(
            _peakMeanDeviationMv);

        Array.Clear(
            _integratedMeanDeviationMvMs);

        Array.Clear(
            _totalFiredCount);

        _direction =
            direction;

        _polarity =
            polarity;

        _durationMs =
            0;

        _frameCount =
            0;

        _totalIlluminatedColumnSamples =
            0;

        _totalPhotoreceptorSamples =
            0;

        _totalTargetSamples =
            0;

        _totalAppliedConnectionSamples =
            0;

        _startP =
            startP;

        _endP =
            startP;

        IsRecording =
            true;
    }

    /// <summary>
    /// Records one biological timestep and the visual input delivered during
    /// that timestep.
    /// </summary>
    public void RecordStep(
        NeuronStateTable state,
        float timestepMs,
        VisualFrameStimulusStatistics? stimulus,
        double presentedEdgeP)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        if (!IsRecording)
        {
            return;
        }

        if (!float.IsFinite(
                timestepMs) ||
            timestepMs <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timestepMs));
        }

        if (!double.IsFinite(
                presentedEdgeP))
        {
            throw new ArgumentOutOfRangeException(
                nameof(presentedEdgeP));
        }

        for (var typeIndex = 0;
             typeIndex < Types.Length;
             typeIndex++)
        {
            var activity =
                MeasurePopulation(
                    state,
                    typeIndex);

            _peakMeanDeviationMv[
                typeIndex] =
                MathF.Max(
                    _peakMeanDeviationMv[
                        typeIndex],
                    activity.MeanAbsoluteVoltageDeviationMv);

            _integratedMeanDeviationMvMs[
                typeIndex] +=
                activity.MeanAbsoluteVoltageDeviationMv *
                timestepMs;

            _totalFiredCount[
                typeIndex] +=
                activity.FiredNeuronCount;
        }

        //
        // Record the sensory exposure independently from neural output.
        //

        if (stimulus is not null)
        {
            var value =
                stimulus.Value;

            _frameCount++;

            _totalIlluminatedColumnSamples +=
                value.ActiveColumnCount;

            _totalPhotoreceptorSamples +=
                value.PhotoreceptorCount;

            _totalTargetSamples +=
                value.UniqueTargetCount;

            _totalAppliedConnectionSamples +=
                value.AppliedConnectionCount;
        }

        _endP =
            presentedEdgeP;

        _durationMs +=
            timestepMs;
    }

    /// <summary>
    /// Completes the current recording and returns immutable results.
    /// </summary>
    public MotionSweepResult Complete()
    {
        if (!IsRecording)
        {
            throw new InvalidOperationException(
                "No motion sweep is currently being recorded.");
        }

        IsRecording =
            false;

        var results =
            new MotionSubtypeSweepResult[
                Types.Length];

        for (var index = 0;
             index < Types.Length;
             index++)
        {
            results[index] =
                new MotionSubtypeSweepResult(
                    Types[index],
                    _peakMeanDeviationMv[index],
                    _integratedMeanDeviationMvMs[index],
                    _totalFiredCount[index]);
        }

        var exposure =
            new MotionSweepExposureStatistics(
                _frameCount,
                _totalIlluminatedColumnSamples,
                _totalPhotoreceptorSamples,
                _totalTargetSamples,
                _totalAppliedConnectionSamples,
                _startP,
                _endP);

        return new MotionSweepResult(
            _direction,
            _polarity,
            _durationMs,
            exposure,
            results);
    }

    /// <summary>
    /// Cancels the current recording without producing results.
    /// </summary>
    public void Cancel()
    {
        IsRecording =
            false;
    }

    /// <summary>
    /// Captures current T4/T5 state without altering experiment results.
    /// </summary>
    public IReadOnlyList<MotionSubtypeActivity> CaptureCurrent(
        NeuronStateTable state)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        var output =
            new MotionSubtypeActivity[
                Types.Length];

        for (var index = 0;
             index < Types.Length;
             index++)
        {
            output[index] =
                MeasurePopulation(
                    state,
                    index);
        }

        return output;
    }

    private MotionSubtypeActivity MeasurePopulation(
        NeuronStateTable state,
        int typeIndex)
    {
        var neuronIndices =
            _neuronIndicesByType[
                typeIndex];

        if (neuronIndices.Length == 0)
        {
            return new MotionSubtypeActivity(
                Types[typeIndex],
                0,
                0,
                0,
                0);
        }

        var absoluteDeviationTotal =
            0f;

        var activeCount =
            0;

        var firedCount =
            0;

        foreach (var neuronIndex in
                 neuronIndices)
        {
            var voltage =
                state.GetMembranePotentialMv(
                    neuronIndex);

            var deviation =
                MathF.Abs(
                    voltage -
                    _restingPotentialMv);

            var fired =
                state.DidFire(
                    neuronIndex);

            absoluteDeviationTotal +=
                deviation;

            if (deviation >=
                    ActiveVoltageThresholdMv ||
                fired)
            {
                activeCount++;
            }

            if (fired)
            {
                firedCount++;
            }
        }

        return new MotionSubtypeActivity(
            Types[typeIndex],
            neuronIndices.Length,
            activeCount,
            firedCount,
            absoluteDeviationTotal /
            neuronIndices.Length);
    }
}

/// <summary>
/// Current aggregate activity of one T4/T5 subtype.
/// </summary>
public readonly record struct MotionSubtypeActivity(
    string Type,
    int TotalNeuronCount,
    int ActiveNeuronCount,
    int FiredNeuronCount,
    float MeanAbsoluteVoltageDeviationMv);

/// <summary>
/// Recorded response of one subtype over an entire sweep.
/// </summary>
public readonly record struct MotionSubtypeSweepResult(
    string Type,
    float PeakMeanAbsoluteVoltageDeviationMv,
    double IntegratedMeanAbsoluteVoltageDeviationMvMs,
    int TotalFiredCount);

/// <summary>
/// Describes the amount of sensory input presented during one sweep.
/// </summary>
public readonly record struct MotionSweepExposureStatistics(
    int FrameCount,
    long TotalIlluminatedColumnSamples,
    long TotalPhotoreceptorSamples,
    long TotalTargetSamples,
    long TotalAppliedConnectionSamples,
    double StartP,
    double EndP)
{
    /// <summary>
    /// Gets the mean illuminated-column count per presented frame.
    /// </summary>
    public double MeanIlluminatedColumnsPerFrame =>
        FrameCount == 0
            ? 0
            : TotalIlluminatedColumnSamples /
              (double)FrameCount;

    /// <summary>
    /// Gets the mean stimulated-photoreceptor count per frame.
    /// </summary>
    public double MeanPhotoreceptorsPerFrame =>
        FrameCount == 0
            ? 0
            : TotalPhotoreceptorSamples /
              (double)FrameCount;

    /// <summary>
    /// Gets the mean number of unique immediate neural targets per frame.
    /// </summary>
    public double MeanTargetsPerFrame =>
        FrameCount == 0
            ? 0
            : TotalTargetSamples /
              (double)FrameCount;
}

/// <summary>
/// Complete result from one directional moving-edge experiment.
/// </summary>
public sealed class MotionSweepResult
{
    public MotionSweepDirection Direction { get; }

    public MotionEdgePolarity Polarity { get; }

    public double DurationMs { get; }

    /// <summary>
    /// Gets statistics describing the sensory exposure delivered during the
    /// sweep.
    /// </summary>
    public MotionSweepExposureStatistics Exposure { get; }

    public IReadOnlyList<MotionSubtypeSweepResult> Subtypes { get; }

    internal MotionSweepResult(
        MotionSweepDirection direction,
        MotionEdgePolarity polarity,
        double durationMs,
        MotionSweepExposureStatistics exposure,
        IReadOnlyList<MotionSubtypeSweepResult> subtypes)
    {
        Direction =
            direction;

        Polarity =
            polarity;

        DurationMs =
            durationMs;

        Exposure =
            exposure;

        Subtypes =
            subtypes;
    }

    /// <summary>
    /// Gets one subtype result when present.
    /// </summary>
    public MotionSubtypeSweepResult? GetSubtype(
        string type)
    {
        foreach (var subtype in
                 Subtypes)
        {
            if (subtype.Type.Equals(
                    type,
                    StringComparison.OrdinalIgnoreCase))
            {
                return subtype;
            }
        }

        return null;
    }
}