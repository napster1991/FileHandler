using System.Diagnostics.CodeAnalysis;

namespace FileHandler.Services;

/// <summary>
/// Implementation of ITempDirectoryManager for creating and cleaning up temporary directories.
/// </summary>
[ExcludeFromCodeCoverage]
public class TempDirectoryManager : ITempDirectoryManager
{
    private readonly ILogger<TempDirectoryManager> _logger;

    public TempDirectoryManager(ILogger<TempDirectoryManager>logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new temporary directory for file sorting operations.
    /// </summary>
    /// <returns>The path to the created temporary directory</returns>
    public string CreateTempDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }

    /// <summary>
    /// Cleans up a temporary directory and all its contents.
    /// Logs warnings if cleanup fails but does not throw exceptions.
    /// </summary>
    /// <param name="tempDirectory">The path to the temporary directory to clean up</param>
    public void CleanupTempDirectory(string tempDirectory)
    {
        try
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to cleanup temp directory: {TempDirectory}", tempDirectory);
        }
    }
}
