namespace FileHandler.Services;

/// <summary>
/// Manages temporary directory creation and cleanup for file sorting operations.
/// </summary>
public interface ITempDirectoryManager
{
    /// <summary>
    /// Creates a new temporary directory for file sorting.
    /// </summary>
    /// <returns>The path to the created temporary directory</returns>
    string CreateTempDirectory();

    /// <summary>
    /// Cleans up a temporary directory, removing all contents.
    /// </summary>
    /// <param name="tempDirectory">The path to the temporary directory to clean up</param>
    void CleanupTempDirectory(string tempDirectory);
}
