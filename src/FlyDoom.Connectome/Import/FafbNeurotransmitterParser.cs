using FlyDoom.Core.Biology;

namespace FlyDoom.Connectome.Import;

/// <summary>
/// Converts FAFB neurotransmitter codes into FlyDoom neurotransmitter types.
/// </summary>
internal static class FafbNeurotransmitterParser
{
    /// <summary>
    /// Parses a neurotransmitter code used by the FAFB dataset.
    /// </summary>
    public static NeurotransmitterType Parse(string? value)
    {
        return value?.Trim().ToUpperInvariant() switch
        {
            "ACH" => NeurotransmitterType.Acetylcholine,
            "GABA" => NeurotransmitterType.Gaba,
            "GLUT" => NeurotransmitterType.Glutamate,
            "DA" => NeurotransmitterType.Dopamine,
            "SER" => NeurotransmitterType.Serotonin,
            "OCT" => NeurotransmitterType.Octopamine,

            null or "" or "UNKNOWN" => NeurotransmitterType.Unknown,

            _ => throw new InvalidDataException($"Unknown FAFB neurotransmitter type: {value}")
        }; 
    }
}