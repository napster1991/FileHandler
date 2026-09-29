using FileHandler.Helpers;

namespace FileHandler.Unit.Helpers;

public class ChunkSizeCalculatorTests
{
    [Fact]
    public void CalculateChunkSizeBytes_SmallFile_ReturnsMinimumChunkSize()
    {
        // Arrange
        long fileSizeBytes = 1024; // 1KB, well below the minimum threshold

        // Act
        long result = ChunkSizeCalculator.CalculateChunkSizeBytes(fileSizeBytes);

        // Assert
        Assert.Equal(Constants.FileSorting.MemoryOptimizedMinChunkSizeBytes, result);
    }

    [Fact]
    public void CalculateChunkSizeBytes_ZeroFileSize_ReturnsMinimumChunkSize()
    {
        // Act
        long result = ChunkSizeCalculator.CalculateChunkSizeBytes(0);

        // Assert
        Assert.Equal(Constants.FileSorting.MemoryOptimizedMinChunkSizeBytes, result);
    }

    [Fact]
    public void CalculateChunkSizeBytes_HugeFile_ReturnsMaximumChunkSize()
    {
        // Arrange
        long fileSizeBytes = long.MaxValue / 2;

        // Act
        long result = ChunkSizeCalculator.CalculateChunkSizeBytes(fileSizeBytes);

        // Assert
        Assert.Equal(Constants.FileSorting.MemoryOptimizedDefaultMaxChunkSizeBytes, result);
    }

    [Fact]
    public void CalculateChunkSizeBytes_MidRangeFile_ReturnsClampedValueWithinBounds()
    {
        // Arrange
        int targetChunkCount = Environment.ProcessorCount * 4;
        long midValue = (Constants.FileSorting.MemoryOptimizedMinChunkSizeBytes
            + Constants.FileSorting.MemoryOptimizedDefaultMaxChunkSizeBytes) / 2;
        long fileSizeBytes = midValue * targetChunkCount;

        // Act
        long result = ChunkSizeCalculator.CalculateChunkSizeBytes(fileSizeBytes);

        // Assert
        Assert.InRange(result,
            Constants.FileSorting.MemoryOptimizedMinChunkSizeBytes,
            Constants.FileSorting.MemoryOptimizedDefaultMaxChunkSizeBytes);
    }

    [Fact]
    public void CalculateChunkSizeBytes_ResultNeverBelowMinimum()
    {
        // Arrange
        long[] sizes = [0, 1, 100, 1024, 1024 * 1024];

        foreach (var size in sizes)
        {
            // Act
            long result = ChunkSizeCalculator.CalculateChunkSizeBytes(size);

            // Assert
            Assert.True(result >= Constants.FileSorting.MemoryOptimizedMinChunkSizeBytes);
        }
    }

    [Fact]
    public void CalculateChunkSizeBytes_ResultNeverAboveMaximum()
    {
        // Arrange
        long[] sizes = [long.MaxValue, long.MaxValue / 2, 10L * 1024 * 1024 * 1024];

        foreach (var size in sizes)
        {
            // Act
            long result = ChunkSizeCalculator.CalculateChunkSizeBytes(size);

            // Assert
            Assert.True(result <= Constants.FileSorting.MemoryOptimizedDefaultMaxChunkSizeBytes);
        }
    }
}
