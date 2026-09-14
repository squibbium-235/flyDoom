using System.IO.Compression;

namespace FlyDoom.Connectome.IO;

/// <summary>
/// Provides helper methods for reading gzip-compressed CSV files.
/// </summary>
public static class GzipCsvFile
{
    /// <summary>
    /// Opens a gzip-compressed file and returns a text reader that
    /// transparently decompresses the file as it is read
    /// </summary>
    /// <param name="path"></param>
    /// <returns>
    /// A <see cref=StreamReader"/> for reading the decompressed contents.
    /// </returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the specified file doesn't exist
    /// </exception>
    public static StreamReader Open(string path)
    {
        if(!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Could not find FlyDoom data file: {path}",
                path);
        }

        // Open the compressed file WITHOUT loading the entire thing into memory
        // This is important because some of the files are like 13 gigabytes
        var fileStream = File.OpenRead(path);

        // Decompress data as it is required rather than all at once
        var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);

        return new StreamReader(gzipStream);
    }
}