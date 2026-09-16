namespace FlyDoom.Runtime;

/// <summary>
/// Locates the FlyDoom repository and its local data directories.
/// </summary>
public static class RepositoryLocator
{
    /// <summary>
    /// Finds the repository root by walking upward until flyDoom.slnx is found.
    /// </summary>
    public static DirectoryInfo FindRepositoryRoot()
    {
        var directory =
            new DirectoryInfo(
                Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var solutionPath =
                Path.Combine(
                    directory.FullName,
                    "flyDoom.slnx");

            if (File.Exists(solutionPath))
            {
                return directory;
            }

            directory =
                directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the FlyDoom repository root.");
    }

    /// <summary>
    /// Gets the raw FAFB v783 dataset directory in the current repository.
    /// </summary>
    public static string GetFafbV783DataDirectory()
    {
        var repositoryRoot =
            FindRepositoryRoot();

        return Path.Combine(
            repositoryRoot.FullName,
            "data",
            "fafb-v783",
            "raw");
    }
}