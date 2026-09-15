using System.Globalization;
using CsvHelper;
using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.IO;

namespace FlyDoom.Connectome.Import;

/// <summary>
/// Reads raw FAFB v783 data from the files supplied by FlyWire/Codex.
/// </summary>
public sealed class FafbDatasetReader
{
    private readonly string _dataDirectory;

    /// <summary>
    /// Initialises a new FAFB dataset reader.
    /// </summary>
    /// <param name="dataDirectory">
    /// Directory containing the raw .csv.gz files.
    /// </param>
    public FafbDatasetReader(string dataDirectory)
    {
        if (!Directory.Exists(dataDirectory))
        {
            throw new DirectoryNotFoundException(
                $"FAFB data directory does not exist: {dataDirectory}");
        }

        _dataDirectory = dataDirectory;
    }

    public IEnumerable<FafbNeuronRecord> ReadNeurons()
        => ReadRecords<FafbNeuronRecord>(
            "neurons.csv.gz");

    public IEnumerable<FafbConnectionRecord> ReadConnections(
        bool includeUnfiltered = false)
        => ReadRecords<FafbConnectionRecord>(
            includeUnfiltered
                ? "connections_princeton_no_threshold.csv.gz"
                : "connections_princeton.csv.gz");

    public IEnumerable<FafbCellStatsRecord> ReadCellStats()
        => ReadRecords<FafbCellStatsRecord>(
            "cell_stats.csv.gz");

    public IEnumerable<FafbClassificationRecord> ReadClassifications()
        => ReadRecords<FafbClassificationRecord>(
            "classification.csv.gz");

    public IEnumerable<FafbColumnAssignmentRecord> ReadColumnAssignments()
        => ReadRecords<FafbColumnAssignmentRecord>(
            "column_assignment.csv.gz");

    public IEnumerable<FafbConnectivityTagRecord> ReadConnectivityTags()
        => ReadRecords<FafbConnectivityTagRecord>(
            "connectivity_tags.csv.gz");

    public IEnumerable<FafbCellTypeRecord> ReadCellTypes()
        => ReadRecords<FafbCellTypeRecord>(
            "consolidated_cell_types.csv.gz");

    public IEnumerable<FafbCoordinateRecord> ReadCoordinates()
        => ReadRecords<FafbCoordinateRecord>(
            "coordinates.csv.gz");

    public IEnumerable<FafbNameRecord> ReadNames()
        => ReadRecords<FafbNameRecord>(
            "names.csv.gz");

    public IEnumerable<FafbProcessedLabelsRecord> ReadProcessedLabels()
        => ReadRecords<FafbProcessedLabelsRecord>(
            "processed_labels.csv.gz");

    public IEnumerable<FafbVisualNeuronTypeRecord> ReadVisualNeuronTypes()
        => ReadRecords<FafbVisualNeuronTypeRecord>(
            "visual_neuron_types.csv.gz");

    /// <summary>
    /// Streams individual synapses from the large Princeton synapse table.
    /// </summary>
    /// <remarks>
    /// This dataset can be extremely large and should normally be streamed,
    /// not materialised into a list.
    /// </remarks>
    public IEnumerable<FafbSynapseRecord> ReadSynapses()
        => ReadRecords<FafbSynapseRecord>(
            "fafb_v783_princeton_synapse_table.csv.gz");

    /// <summary>
    /// Streams records from one gzip-compressed CSV file.
    /// </summary>
    private IEnumerable<T> ReadRecords<T>(
        string fileName)
    {
        var path =
            Path.Combine(
                _dataDirectory,
                fileName);

        using var reader =
            GzipCsvFile.Open(path);

        using var csv =
            new CsvReader(
                reader,
                CultureInfo.InvariantCulture);

        foreach (var record in csv.GetRecords<T>())
        {
            yield return record;
        }
    }
}