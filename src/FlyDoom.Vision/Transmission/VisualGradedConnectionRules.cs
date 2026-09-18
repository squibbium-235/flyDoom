namespace FlyDoom.Vision.Transmission;

/// <summary>
/// Contains the explicitly supported graded visual connection rules used by
/// the current reference model.
/// </summary>
/// <remarks>
/// Structural connectivity alone does not establish functional synaptic sign.
///
/// Rules are therefore added only where the current literature gives us a
/// defensible functional interpretation. The real FAFB connectome still
/// determines which individual neurons are actually connected.
/// </remarks>
public static class VisualGradedConnectionRules
{
    private static readonly VisualGradedConnectionRule[] Rules =
        BuildRules();

    /// <summary>
    /// Gets all graded visual connection rules currently represented by the
    /// reference model.
    /// </summary>
    public static IReadOnlyList<VisualGradedConnectionRule> All =>
        Rules;

    private static VisualGradedConnectionRule[] BuildRules()
    {
        var rules =
            new List<VisualGradedConnectionRule>();

        //
        // First-order lamina pathways.
        //
        // L1 is glutamatergic. At major ON-pathway targets such as Mi1 and Tm3,
        // glutamate-gated chloride receptors produce an inhibitory functional
        // effect.
        //
        // A light increment hyperpolarises L1. Reducing that inhibitory output
        // therefore disinhibits Mi1/Tm3, producing the expected ON response.
        //

        AddRule(
            rules,
            "L1",
            "Mi1",
            -1f,
            "L1 glutamatergic input is inhibitory at the Mi1 ON-pathway synapse.");

        AddRule(
            rules,
            "L1",
            "Tm3",
            -1f,
            "L1 glutamatergic input is inhibitory at the Tm3 ON-pathway synapse.");

        //
        // L2 and L3 provide major cholinergic routes into the OFF pathway.
        //

        AddRule(
            rules,
            "L2",
            "Tm1",
            1f,
            "L2 provides cholinergic sign-preserving input to Tm1.");

        AddRule(
            rules,
            "L2",
            "Tm2",
            1f,
            "L2 provides cholinergic sign-preserving input to Tm2.");

        AddRule(
            rules,
            "L3",
            "Tm9",
            1f,
            "L3 provides cholinergic sign-preserving input to Tm9.");

        AddRule(
            rules,
            "L3",
            "Mi9",
            1f,
            "L3 provides sign-preserving drive into the Mi9 branch of the ON-motion circuit.");

        //
        // T4 ON-motion detector inputs.
        //
        // FAFB structural connectivity decides which individual Mi/Tm neuron
        // reaches which T4 neuron. These rules merely provide the functional
        // interpretation of those existing edges.
        //

        AddRules(
            rules,
            "Mi1",
            1f,
            "Mi1 is cholinergic and provides excitatory input to T4.",
            "T4a",
            "T4b",
            "T4c",
            "T4d");

        AddRules(
            rules,
            "Tm3",
            1f,
            "Tm3 is cholinergic and provides excitatory input to T4.",
            "T4a",
            "T4b",
            "T4c",
            "T4d");

        //
        // Mi9 is glutamatergic. Available physiological and receptor evidence
        // supports an inhibitory role at T4, but this is less direct than the
        // cholinergic Mi1/Tm3 case.
        //
        // Keep that uncertainty documented instead of quietly pretending this
        // sign came directly out of the connectome.
        //

        AddRules(
            rules,
            "Mi9",
            -1f,
            "Mi9 glutamatergic input is represented as inhibitory at T4 based on current physiological and receptor evidence.",
            "T4a",
            "T4b",
            "T4c",
            "T4d");

        //
        // Mi4 is a delayed GABAergic T4 input and is represented as
        // inhibitory. It may not yet become active because its own upstream
        // temporal pathway has not been modelled.
        //

        AddRules(
            rules,
            "Mi4",
            -1f,
            "Mi4 is GABAergic and provides inhibitory input to T4.",
            "T4a",
            "T4b",
            "T4c",
            "T4d");

        //
        // T5 OFF-motion detector inputs.
        //
        // Tm1, Tm2, Tm4 and Tm9 are the four major columnar feed-forward
        // inputs to T5 and are cholinergic.
        //

        AddRules(
            rules,
            "Tm1",
            1f,
            "Tm1 provides cholinergic excitatory input to T5.",
            "T5a",
            "T5b",
            "T5c",
            "T5d");

        AddRules(
            rules,
            "Tm2",
            1f,
            "Tm2 provides cholinergic excitatory input to T5.",
            "T5a",
            "T5b",
            "T5c",
            "T5d");

        AddRules(
            rules,
            "Tm4",
            1f,
            "Tm4 provides cholinergic excitatory input to T5.",
            "T5a",
            "T5b",
            "T5c",
            "T5d");

        AddRules(
            rules,
            "Tm9",
            1f,
            "Tm9 provides cholinergic excitatory input to T5.",
            "T5a",
            "T5b",
            "T5c",
            "T5d");

        return rules.ToArray();
    }

    private static void AddRule(
        List<VisualGradedConnectionRule> rules,
        string presynapticType,
        string postsynapticType,
        float polarity,
        string evidenceNote)
    {
        rules.Add(
            new VisualGradedConnectionRule(
                presynapticType,
                postsynapticType,
                polarity,
                evidenceNote));
    }

    private static void AddRules(
        List<VisualGradedConnectionRule> rules,
        string presynapticType,
        float polarity,
        string evidenceNote,
        params string[] postsynapticTypes)
    {
        foreach (var postsynapticType in
                 postsynapticTypes)
        {
            AddRule(
                rules,
                presynapticType,
                postsynapticType,
                polarity,
                evidenceNote);
        }
    }
}