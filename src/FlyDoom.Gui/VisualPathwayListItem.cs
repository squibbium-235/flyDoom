using FlyDoom.Vision.Diagnostics;

namespace FlyDoom.Gui;

/// <summary>
/// Formats visual pathway telemetry for display in the Avalonia interface.
/// </summary>
public sealed class VisualPathwayListItem
{
    public string Stage { get; }

    public string Type { get; }

    public string TotalText { get; }

    public string ActiveText { get; }

    public string FiredText { get; }

    public string MaximumInputText { get; }

    public string VoltageRangeText { get; }

    public VisualPathwayListItem(
        VisualPathwayActivity activity)
    {
        Stage =
            activity.Stage;

        Type =
            activity.Type;

        TotalText =
            activity.TotalNeuronCount.ToString(
                "N0");

        ActiveText =
            activity.ActiveNeuronCount.ToString(
                "N0");

        FiredText =
            activity.FiredNeuronCount.ToString(
                "N0");

        MaximumInputText =
            activity.MaximumAbsoluteInputMv.ToString(
                "F2");

        VoltageRangeText =
            float.IsNaN(
                activity.MinimumMembranePotentialMv)
                ? "-"
                : $"{activity.MinimumMembranePotentialMv:F1}.." +
                  $"{activity.MaximumMembranePotentialMv:F1}";
    }
}