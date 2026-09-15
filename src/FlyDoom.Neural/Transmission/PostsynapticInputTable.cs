using FlyDoom.Connectome.Model;

namespace FlyDoom.Neural.Transmission;

/// <summary>
/// Stores the total number of anatomical input synapses received by
/// each neuron.
/// </summary>
public sealed class PostsynapticInputTable
{
    private readonly long[] _totalIncomingSynapses;

    /// <summary>
    /// Gets the number of neurons represented by the table.
    /// </summary>
    public int Count => _totalIncomingSynapses.Length;

    private PostsynapticInputTable(
        long[] totalIncomingSynapses)
    {
        _totalIncomingSynapses =
            totalIncomingSynapses;
    }

    /// <summary>
    /// Builds postsynaptic input totals from a compact connectome.
    /// </summary>
    public static PostsynapticInputTable Build(
        CompactConnectome connectome)
    {
        ArgumentNullException.ThrowIfNull(
            connectome);

        var totals =
            new long[connectome.NeuronCount];

        for (var presynapticIndex = 0;
             presynapticIndex < connectome.NeuronCount;
             presynapticIndex++)
        {
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
                    targets[connectionIndex];

                totals[postsynapticIndex] =
                    checked(
                        totals[postsynapticIndex] +
                        synapseCounts[connectionIndex]);
            }
        }

        return new PostsynapticInputTable(
            totals);
    }

    /// <summary>
    /// Gets the total number of anatomical synapses received by a neuron.
    /// </summary>
    public long GetTotalIncomingSynapses(
        int neuronIndex)
    {
        if ((uint)neuronIndex >= (uint)Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(neuronIndex));
        }

        return _totalIncomingSynapses[
            neuronIndex];
    }
}