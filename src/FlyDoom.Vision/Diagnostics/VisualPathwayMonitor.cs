using FlyDoom.Neural.Simulation;
using FlyDoom.Vision.Model;

namespace FlyDoom.Vision.Diagnostics;

/// <summary>
/// Measures aggregate activity through selected stages of the Drosophila
/// visual motion pathways.
/// </summary>
/// <remarks>
/// "Active" does not mean "spiking".
///
/// Much of the fly optic lobe communicates using graded membrane potentials.
/// A monitored neuron is therefore considered active when any of the following
/// is true:
///
/// - it fired during the current timestep;
/// - it has meaningful current synaptic drive; or
/// - its membrane potential is meaningfully displaced from rest.
///
/// This distinction is important for L1/L2/L3 and the Mi/Tm neurons upstream
/// of T4/T5, all of which can carry useful information without producing
/// conventional action potentials.
/// </remarks>
public sealed class VisualPathwayMonitor
{
    private const float ActiveInputThresholdMv =
        0.05f;

    private const float ActiveVoltageDeviationThresholdMv =
        0.05f;

    private static readonly PathwayDefinition[] Definitions =
    [
        new(
            "Retina",
            "R1-6"),

        new(
            "Retina",
            "R7"),

        new(
            "Retina",
            "R8"),

        new(
            "Lamina",
            "L1"),

        new(
            "Lamina",
            "L2"),

        new(
            "Lamina",
            "L3"),

        new(
            "ON relay",
            "Mi1"),

        new(
            "ON relay",
            "Tm3"),

        new(
            "ON relay",
            "Mi4"),

        new(
            "ON relay",
            "Mi9"),

        new(
            "OFF relay",
            "Tm1"),

        new(
            "OFF relay",
            "Tm2"),

        new(
            "OFF relay",
            "Tm4"),

        new(
            "OFF relay",
            "Tm9"),

        new(
            "ON detector",
            "T4a"),

        new(
            "ON detector",
            "T4b"),

        new(
            "ON detector",
            "T4c"),

        new(
            "ON detector",
            "T4d"),

        new(
            "OFF detector",
            "T5a"),

        new(
            "OFF detector",
            "T5b"),

        new(
            "OFF detector",
            "T5c"),

        new(
            "OFF detector",
            "T5d")
    ];

    private readonly int[][]
        _neuronIndicesByDefinition;

    private readonly float
        _restingPotentialMv;

    /// <summary>
    /// Builds an index of the visual populations monitored by the GUI.
    /// </summary>
    /// <param name="catalog">
    /// Visual-system annotation aligned with simulation neuron indices.
    /// </param>
    /// <param name="restingPotentialMv">
    /// Reference resting potential used when deciding whether a graded neuron
    /// is currently active.
    /// </param>
    public VisualPathwayMonitor(
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

        var definitionIndexByType =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        for (var definitionIndex = 0;
             definitionIndex < Definitions.Length;
             definitionIndex++)
        {
            definitionIndexByType.Add(
                Definitions[
                    definitionIndex]
                    .Type,
                definitionIndex);
        }

        //
        // Build the population lists once.
        //
        // The GUI can then sample pathway state every refresh without scanning
        // and comparing type strings for all 139,255 neurons.
        //

        var working =
            new List<int>[
                Definitions.Length];

        for (var definitionIndex = 0;
             definitionIndex < working.Length;
             definitionIndex++)
        {
            working[
                definitionIndex] =
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
                !definitionIndexByType.TryGetValue(
                    type,
                    out var definitionIndex))
            {
                continue;
            }

            working[
                definitionIndex]
                .Add(
                    neuronIndex);
        }

        _neuronIndicesByDefinition =
            new int[
                Definitions.Length][];

        for (var definitionIndex = 0;
             definitionIndex < working.Length;
             definitionIndex++)
        {
            _neuronIndicesByDefinition[
                definitionIndex] =
                working[
                    definitionIndex]
                    .ToArray();
        }
    }

    /// <summary>
    /// Captures aggregate activity for every monitored visual population.
    /// </summary>
    public IReadOnlyList<VisualPathwayActivity> Capture(
        NeuronStateTable state)
    {
        ArgumentNullException.ThrowIfNull(
            state);

        var output =
            new VisualPathwayActivity[
                Definitions.Length];

        for (var definitionIndex = 0;
             definitionIndex < Definitions.Length;
             definitionIndex++)
        {
            var definition =
                Definitions[
                    definitionIndex];

            var neuronIndices =
                _neuronIndicesByDefinition[
                    definitionIndex];

            var activeCount =
                0;

            var firedCount =
                0;

            var maximumAbsoluteInputMv =
                0f;

            var maximumAbsoluteVoltageDeviationMv =
                0f;

            var minimumVoltageMv =
                float.PositiveInfinity;

            var maximumVoltageMv =
                float.NegativeInfinity;

            foreach (var neuronIndex in
                     neuronIndices)
            {
                var input =
                    state.GetSynapticInputMv(
                        neuronIndex);

                var voltage =
                    state.GetMembranePotentialMv(
                        neuronIndex);

                var fired =
                    state.DidFire(
                        neuronIndex);

                var voltageDeviation =
                    voltage -
                    _restingPotentialMv;

                //
                // Graded neurons can be carrying information even after their
                // immediate synaptic input has become very small.
                //
                // Counting membrane displacement therefore gives a much more
                // truthful picture of activity in the optic lobe than using
                // spikes alone.
                //

                if (fired ||
                    MathF.Abs(
                        input) >=
                    ActiveInputThresholdMv ||
                    MathF.Abs(
                        voltageDeviation) >=
                    ActiveVoltageDeviationThresholdMv)
                {
                    activeCount++;
                }

                if (fired)
                {
                    firedCount++;
                }

                maximumAbsoluteInputMv =
                    MathF.Max(
                        maximumAbsoluteInputMv,
                        MathF.Abs(
                            input));

                maximumAbsoluteVoltageDeviationMv =
                    MathF.Max(
                        maximumAbsoluteVoltageDeviationMv,
                        MathF.Abs(
                            voltageDeviation));

                minimumVoltageMv =
                    MathF.Min(
                        minimumVoltageMv,
                        voltage);

                maximumVoltageMv =
                    MathF.Max(
                        maximumVoltageMv,
                        voltage);
            }

            if (neuronIndices.Length == 0)
            {
                minimumVoltageMv =
                    float.NaN;

                maximumVoltageMv =
                    float.NaN;
            }

            output[
                definitionIndex] =
                new VisualPathwayActivity(
                    definition.Stage,
                    definition.Type,
                    neuronIndices.Length,
                    activeCount,
                    firedCount,
                    maximumAbsoluteInputMv,
                    maximumAbsoluteVoltageDeviationMv,
                    minimumVoltageMv,
                    maximumVoltageMv);
        }

        return output;
    }

    private readonly record struct PathwayDefinition(
        string Stage,
        string Type);
}

/// <summary>
/// Describes the current aggregate state of one monitored visual population.
/// </summary>
public readonly record struct VisualPathwayActivity(
    string Stage,
    string Type,
    int TotalNeuronCount,
    int ActiveNeuronCount,
    int FiredNeuronCount,
    float MaximumAbsoluteInputMv,
    float MaximumAbsoluteVoltageDeviationMv,
    float MinimumMembranePotentialMv,
    float MaximumMembranePotentialMv);