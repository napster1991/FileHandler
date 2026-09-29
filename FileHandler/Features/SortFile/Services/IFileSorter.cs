namespace FileHandler.Features.SortFile.Services;

/// <summary>
/// Interface for file sorting implementations with different optimization strategies.
/// </summary>
public interface IFileSorter
{
    /// <summary>
    /// Sorts a large file using the implemented strategy and writes the result to an output file.
    /// The chunk size used during sorting is calculated automatically based on the input file's size.
    /// </summary>
    /// <param name="inputFilePath">Path to the input file</param>
    /// <param name="outputFilePath">Path to the output file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of lines processed</returns>
    Task<long> SortFileAsync(string inputFilePath, string outputFilePath,
        CancellationToken cancellationToken = default);
}
