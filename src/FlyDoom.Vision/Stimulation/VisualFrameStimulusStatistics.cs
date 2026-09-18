namespace FlyDoom.Vision.Stimulation;

/// <summary>
/// Summarises one complete spatial visual-frame stimulus.
/// </summary>
public readonly record struct VisualFrameStimulusStatistics(
    int ActiveColumnCount,
    int PhotoreceptorCount,
    long AppliedConnectionCount,
    int UniqueTargetCount,
    float MostNegativeInputAmplitudeMv);