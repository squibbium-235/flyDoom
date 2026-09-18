namespace FlyDoom.Vision.Transmission;

/// <summary>
/// Describes one evidence-supported graded visual connection rule.
/// </summary>
/// <param name="PresynapticType">
/// Visual neuron type producing graded output.
/// </param>
/// <param name="PostsynapticType">
/// Visual neuron type receiving that output.
/// </param>
/// <param name="Polarity">
/// Sign applied to the presynaptic membrane deviation.
/// </param>
/// <param name="EvidenceNote">
/// Short explanation of why the rule exists.
/// </param>
public readonly record struct VisualGradedConnectionRule(
    string PresynapticType,
    string PostsynapticType,
    float Polarity,
    string EvidenceNote);