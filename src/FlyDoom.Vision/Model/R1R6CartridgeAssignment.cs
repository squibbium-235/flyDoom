namespace FlyDoom.Vision.Model;

/// <summary>
/// Describes the inferred lamina cartridge targeted by one R1-R6
/// photoreceptor.
/// </summary>
/// <remarks>
/// The assignment is inferred from structural connectivity to column-assigned
/// L1 and L2 neurons.
///
/// L1 and L2 are used as independent landmarks because both are canonical
/// postsynaptic partners of R1-R6 photoreceptors in a lamina cartridge.
/// </remarks>
public readonly record struct R1R6CartridgeAssignment(
    int NeuronIndex,
    string Hemisphere,
    string ColumnId,
    int L1SynapseCount,
    int L2SynapseCount,
    int TotalLandmarkSynapseCount)
{
    /// <summary>
    /// Gets the number of L1 and L2 landmark synapses supporting the selected
    /// column.
    /// </summary>
    public int SupportingLandmarkSynapseCount =>
        L1SynapseCount +
        L2SynapseCount;

    /// <summary>
    /// Gets the fraction of all observed L1/L2 landmark synapses supporting
    /// the selected column.
    /// </summary>
    /// <remarks>
    /// This is diagnostic evidence rather than a calibrated biological
    /// probability.
    /// </remarks>
    public double SupportFraction =>
        TotalLandmarkSynapseCount <= 0
            ? 0
            : SupportingLandmarkSynapseCount /
              (double)TotalLandmarkSynapseCount;
}