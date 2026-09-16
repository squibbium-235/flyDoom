namespace FlyDoom.Gui;

/// <summary>
/// Represents one neuron displayed in the current-activity list.
/// </summary>
public sealed class ActiveNeuronListItem
{
    /// <summary>
    /// Gets the compact simulation index of the neuron.
    /// </summary>
    public int NeuronIndex { get; }

    /// <summary>
    /// Gets the neuron's current synaptic input.
    /// </summary>
    public float Input { get; }

    /// <summary>
    /// Gets the neuron's current membrane potential.
    /// </summary>
    public float MembranePotential { get; }

    /// <summary>
    /// Gets whether the neuron fired during the most recent timestep.
    /// </summary>
    public bool Fired { get; }

    /// <summary>
    /// Gets the human-readable neuron name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the annotated neuron type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the formatted input value used by the activity list.
    /// </summary>
    public string InputText =>
        Input.ToString("F2");

    /// <summary>
    /// Gets the formatted membrane potential used by the activity list.
    /// </summary>
    public string MembranePotentialText =>
        MembranePotential.ToString("F2");

    /// <summary>
    /// Gets the combined neuron name and type used by the activity list.
    /// </summary>
    public string IdentityText =>
        Fired
            ? $"⚡ {Name} [{Type}]"
            : $"{Name} [{Type}]";

    public ActiveNeuronListItem(
        int neuronIndex,
        float input,
        float membranePotential,
        bool fired,
        string name,
        string type)
    {
        NeuronIndex =
            neuronIndex;

        Input =
            input;

        MembranePotential =
            membranePotential;

        Fired =
            fired;

        Name =
            name;

        Type =
            type;
    }
}