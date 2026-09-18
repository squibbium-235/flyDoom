using FlyDoom.Vision.Diagnostics;

namespace FlyDoom.Gui;

/// <summary>
/// Formats motion-experiment measurements for the GUI.
/// </summary>
public sealed class MotionSweepListItem
{
    public string Type { get; }

    public string CurrentText { get; }

    public string IncreasingText { get; }

    public string DecreasingText { get; }

    public string DirectionSelectivityText { get; }

    public string SpikesText { get; }

    public MotionSweepListItem(
        MotionSubtypeActivity current,
        MotionSubtypeSweepResult? increasing,
        MotionSubtypeSweepResult? decreasing)
    {
        Type =
            current.Type;

        CurrentText =
            current
                .MeanAbsoluteVoltageDeviationMv
                .ToString(
                    "F3");

        IncreasingText =
            increasing is null
                ? "-"
                : increasing.Value
                    .IntegratedMeanAbsoluteVoltageDeviationMvMs
                    .ToString(
                        "F1");

        DecreasingText =
            decreasing is null
                ? "-"
                : decreasing.Value
                    .IntegratedMeanAbsoluteVoltageDeviationMvMs
                    .ToString(
                        "F1");

        DirectionSelectivityText =
            CalculateDirectionSelectivity(
                increasing,
                decreasing);

        SpikesText =
            FormatSpikes(
                increasing,
                decreasing);
    }

    private static string CalculateDirectionSelectivity(
        MotionSubtypeSweepResult? increasing,
        MotionSubtypeSweepResult? decreasing)
    {
        if (increasing is null ||
            decreasing is null)
        {
            return "-";
        }

        var positive =
            increasing.Value
                .IntegratedMeanAbsoluteVoltageDeviationMvMs;

        var negative =
            decreasing.Value
                .IntegratedMeanAbsoluteVoltageDeviationMvMs;

        var denominator =
            positive +
            negative;

        if (denominator <=
            0.000001)
        {
            return "0.000";
        }

        //
        // Positive values indicate stronger response to increasing P.
        // Negative values indicate stronger response to decreasing P.
        //

        var dsi =
            (positive -
             negative) /
            denominator;

        return dsi.ToString(
            "+0.000;-0.000;0.000");
    }

    private static string FormatSpikes(
        MotionSubtypeSweepResult? increasing,
        MotionSubtypeSweepResult? decreasing)
    {
        var increasingText =
            increasing is null
                ? "-"
                : increasing.Value
                    .TotalFiredCount
                    .ToString(
                        "N0");

        var decreasingText =
            decreasing is null
                ? "-"
                : decreasing.Value
                    .TotalFiredCount
                    .ToString(
                        "N0");

        return $"{increasingText} / {decreasingText}";
    }
}