namespace FileHandler.Helpers;

/// <summary>
/// Calculates an appropriate chunk size for external sorting based on input file size.
/// </summary>
public static class ChunkSizeCalculator
{
    /// <summary>
    /// Calculates an appropriate chunk size based on the input file's size, aiming for a
    /// number of chunks proportional to available processors while staying within the
    /// configured minimum/maximum chunk size bounds.
    /// </summary>
    public static long CalculateChunkSizeBytes(long fileSizeBytes)
    {
        int targetChunkCount = Environment.ProcessorCount * 4;
        long calculatedChunkSize = fileSizeBytes / targetChunkCount;

        var result = Math.Clamp(
            calculatedChunkSize,
            Constants.FileSorting.MemoryOptimizedMinChunkSizeBytes,
            Constants.FileSorting.MemoryOptimizedDefaultMaxChunkSizeBytes);

        return Math.Clamp(
            calculatedChunkSize,
            Constants.FileSorting.MemoryOptimizedMinChunkSizeBytes,
            Constants.FileSorting.MemoryOptimizedDefaultMaxChunkSizeBytes);
    }
}
