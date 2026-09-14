using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using FlyDoom.Connectome.Import.Records;
using FlyDoom.Connectome.IO;

namespace FlyDoom.Connectome.Import;

/// <summary>
/// Reads raw FAFB v783 connectome data from the flies supplied by FlyWire/Codex
/// </summary>
public sealed class FafbDatasetReader
{
    private readonly string _dataDirectory;

    /// <summary>
    /// Initialises a new FAFB dataset reader.
    /// </summary>
    /// <param name="dataDirectory">
    /// Dir containing the raw .csv.gz files.
    /// </param>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when the supplied data directory doesnt exist
    /// </exception>
    public FafbDatasetReader(string dataDirectory)
    {
        if(!Directory.Exists(dataDirectory))
        {
            throw new DirectoryNotFoundException(
                $"FAFB data directory does not exist: {dataDirectory}");
        }

        _dataDirectory = dataDirectory;
    }

    /// <summary>
    /// Streams neuron records from neurons.csv.gz
    /// </summary>
    /// <returns>
    /// An enumerable sequence of FAFB neuron records.
    /// </returns>
    public IEnumerable<FafbNeuronRecord> ReadNeurons()
    {
        var path = Path.Combine(_dataDirectory, "neurons.csv.gz");

        using var reader = GzipCsvFile.Open(path);

        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // Yield each row rather than the entire file immediatley
        // this keeps the reader usable on larger datasets
        foreach(var record in csv.GetRecords<FafbNeuronRecord>())
        {
            yield return record;
        }
    }

    /// <summary>
    /// Streams neuron-to-neuron connections From the priceton conenctivity dataset.
    /// </summary>
    /// <param name="includeUnfiltered">
    /// When true, reads the no-threshhold connectivity dataset.
    /// Otherwise, the filtered dataset is used.
    /// </param>
    /// <returns>
    /// an enumaerable sequence of FAFB connection records.
    /// </returns>
    public IEnumerable<FafbConnectionRecord> ReadConnections(
        bool includeUnfiltered = false)
    {
        var fileName = includeUnfiltered
            ? "connections_princeton_no_threshold.csv.gz"
            : "connections_princeton.csv.gz";

        var path = Path.Combine(_dataDirectory, fileName);

        using var reader = GzipCsvFile.Open(path);

        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // The connection dataset contains millions of rows
        // stream records so that we dont allocate one enormous list
        // just iterate over the dataset, ez
        foreach(var record in csv.GetRecords<FafbConnectionRecord>())
        {
            yield return record;
        }
    }
}