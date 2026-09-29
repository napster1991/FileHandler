namespace FileHandler.Services;

/// <summary>
/// Abstraction for merging already-sorted chunk files into a single output file.
/// </summary>
public interface IChunkMerger
{
    /// <summary>
    /// Merges the given sorted chunk files directly into a single output file.
    /// </summary>
    Task MergeAsync(List<string> chunkFiles, string outputFilePath, CancellationToken cancellationToken);
}
