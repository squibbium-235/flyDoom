using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.Model;
using FlyDoom.Core.Biology;
using FlyDoom.Connectome.Import;

namespace FlyDoom.Connectome.Build;

/// <summary>
/// Builds per-neuron metadata aligned with compact simulation indices.
/// </summary>
public static class CompactNeuronTableBuilder
{
    public static CompactNeuronTable Build(
        IReadOnlyList<FafbNeuronRecord> neurons,
        NeuronIndexMap neuronIndexMap)
    {
        ArgumentNullException.ThrowIfNull(neurons);
        ArgumentNullException.ThrowIfNull(neuronIndexMap);

        var count = neuronIndexMap.Count;

        var neurotransmitterTypes =
            new NeurotransmitterType[count];

        var neurotransmitterConfidence =
            new float[count];

        var dopamineScores = new float[count];
        var serotoninScores = new float[count];
        var gabaScores = new float[count];
        var glutamateScores = new float[count];
        var acetylcholineScores = new float[count];
        var octopamineScores = new float[count];

        foreach (var neuron in neurons)
        {
            if (!neuronIndexMap.TryGetIndex(
                    neuron.RootId,
                    out var index))
            {
                throw new InvalidDataException(
                    $"Unknown neuron root ID: {neuron.RootId}");
            }

            neurotransmitterTypes[index] =
                FafbNeurotransmitterParser.Parse(
                    neuron.NeurotransmitterType);

            neurotransmitterConfidence[index] =
                ToFloat(neuron.NeurotransmitterTypeScore);

            dopamineScores[index] =
                ToFloat(neuron.DopamineAverage);

            serotoninScores[index] =
                ToFloat(neuron.SerotoninAverage);

            gabaScores[index] =
                ToFloat(neuron.GabaAverage);

            glutamateScores[index] =
                ToFloat(neuron.GlutamateAverage);

            acetylcholineScores[index] =
                ToFloat(neuron.AcetylcholineAverage);

            octopamineScores[index] =
                ToFloat(neuron.OctopamineAverage);
        }

        return new CompactNeuronTable(
            neurotransmitterTypes,
            neurotransmitterConfidence,
            dopamineScores,
            serotoninScores,
            gabaScores,
            glutamateScores,
            acetylcholineScores,
            octopamineScores);
    }

    private static float ToFloat(double? value)
    {
        return value.HasValue
            ? checked((float)value.Value)
            : float.NaN;
    }
}