namespace FlyDoom.Neural.Simulation;

/// <summary>
/// Describes neural activity observed during one simulation timestep.
/// </summary>
public readonly record struct NeuralStepStatistics(
    double SimulationTimeMs,
    int FiredNeuronCount,
    int ActiveSynapticNeuronCount,
    long PropagatedConnectionCount,
    float MaximumAbsoluteSynapticInputMv,
    float MinimumMembranePotentialMv,
    float MaximumMembranePotentialMv);