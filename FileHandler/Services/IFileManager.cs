namespace FileHandler.Services;

/// <summary>
/// Abstracts file system operations for file sorting, enabling testability through dependency injection.
/// </summary>
public interface IFileManager
{
    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="path">The file path to check</param>
    /// <returns>True if the file exists; otherwise, false</returns>
    bool FileExists(string path);

    /// <summary>
    /// Gets the size, in bytes, of a file.
    /// </summary>
    /// <param name="path">The file path to inspect</param>
    /// <returns>The size of the file in bytes</returns>
    long GetFileLength(string path);

    /// <summary>
    /// Opens a file for reading.
    /// </summary>
    /// <param name="path">The file path to open</param>
    /// <returns>A FileStream for reading the file</returns>
    FileStream OpenRead(string path);

    /// <summary>
    /// Creates a new file for writing. Overwrites if file exists.
    /// </summary>
    /// <param name="path">The file path to create</param>
    /// <returns>A FileStream for writing to the file</returns>
    FileStream Create(string path);

    /// <summary>
    /// Copies a file from source to destination.
    /// </summary>
    /// <param name="sourceFileName">The source file path</param>
    /// <param name="destFileName">The destination file path</param>
    /// <param name="overwrite">Whether to overwrite if destination exists</param>
    void Copy(string sourceFileName, string destFileName, bool overwrite = false);

    /// <summary>
    /// Moves (renames) a file from source to destination.
    /// </summary>
    /// <param name="sourceFileName">The source file path</param>
    /// <param name="destFileName">The destination file path</param>
    /// <param name="overwrite">Whether to overwrite if destination exists</param>
    void Move(string sourceFileName, string destFileName, bool overwrite = false);

    /// <summary>
    /// Deletes a file.
    /// </summary>
    /// <param name="path">The file path to delete</param>
    void Delete(string path);

    /// <summary>
    /// Creates a directory at the specified path.
    /// </summary>
    /// <param name="path">The directory path to create</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Deletes a directory and optionally its contents.
    /// </summary>
    /// <param name="path">The directory path to delete</param>
    /// <param name="recursive">Whether to recursively delete all contents</param>
    void DeleteDirectory(string path, bool recursive = false);

    /// <summary>
    /// Writes text to a file.
    /// </summary>
    /// <param name="path">The file path to write to</param>
    /// <param name="contents">The text content to write</param>
    void WriteAllText(string path, string contents);

    /// <summary>
    /// Reads all text from a file.
    /// </summary>
    /// <param name="path">The file path to read from</param>
    /// <returns>The file contents as a string</returns>
    string ReadAllText(string path);

    /// <summary>
    /// Gets the temporary path for the system.
    /// </summary>
    /// <returns>The system's temporary directory path</returns>
    string GetTempPath();

    /// <summary>
    /// Gets the directory name from a file path.
    /// </summary>
    /// <param name="path">The file path</param>
    /// <returns>The directory name of the path</returns>
    string GetDirectoryName(string path);

    /// <summary>
    /// Gets the file name without extension from a path.
    /// </summary>
    /// <param name="path">The file path</param>
    /// <returns>The file name without extension</returns>
    string GetFileNameWithoutExtension(string path);

    /// <summary>
    /// Combines path segments into a single path.
    /// </summary>
    /// <param name="paths">The path segments to combine</param>
    /// <returns>The combined path</returns>
    string Combine(params string[] paths);
}
