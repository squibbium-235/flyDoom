namespace FlyDoom.Core.Biology;

/// <summary>
/// Identifies a neurotransmitter represented by the connectome dataset.
/// </summary>
public enum NeurotransmitterType : byte
{
    Unknown = 0,
    Acetylcholine = 1,
    Gaba = 2,
    Glutamate = 3,
    Dopamine = 4,
    Serotonin = 5,
    Octopamine = 6
}