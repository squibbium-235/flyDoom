using System.Globalization;
using System.Text.RegularExpressions;

namespace FlyDoom.Connectome.Import;

/// <summary>
/// Parses the coordinate representation used by FAFB coordinate records.
/// </summary>
public static class FafbPositionParser
{
    private static readonly Regex NumberPattern =
        new(
            @"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?",
            RegexOptions.CultureInvariant);

    /// <summary>
    /// Parses a raw position into three numeric coordinates.
    /// </summary>
    public static (double X, double Y, double Z) Parse(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException(
                "Position cannot be empty.");
        }

        var matches =
            NumberPattern.Matches(value);

        if (matches.Count != 3)
        {
            throw new FormatException(
                $"Expected three coordinate values but found " +
                $"{matches.Count}: {value}");
        }

        return (
            ParseNumber(matches[0].Value),
            ParseNumber(matches[1].Value),
            ParseNumber(matches[2].Value));
    }

    private static double ParseNumber(
        string value)
    {
        return double.Parse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
    }
}