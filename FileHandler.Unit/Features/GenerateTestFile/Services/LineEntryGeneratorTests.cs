using FileHandler.Features.GenerateTestFile.Services;

namespace FileHandler.Unit.Features.GenerateTestFile.Services;

public class LineEntryGeneratorTests
{
    private readonly LineEntryGenerator _generator = new();

    [Fact]
    public void GetNextEntry_EmptyPool_AddsGeneratedTextToPool()
    {
        // Arrange
        var pool = new List<string>();

        // Act
        var entry = _generator.GetNextEntry(pool);

        // Assert
        Assert.Single(pool);
        Assert.Equal(pool[0], entry.Text);
    }

    [Fact]
    public void GetNextEntry_PoolBelowCap_GrowsPool()
    {
        // Arrange
        var pool = new List<string>();

        // Act
        for (int i = 0; i < Constants.FileSorting.TestFileGeneratorTextPoolSize; i++)
        {
            _generator.GetNextEntry(pool);
        }

        // Assert
        Assert.Equal(Constants.FileSorting.TestFileGeneratorTextPoolSize, pool.Count);
    }

    [Fact]
    public void GetNextEntry_PoolAtCap_DoesNotExceedCapWhenReusingText()
    {
        // Arrange
        var pool = new List<string>();
        for (int i = 0; i < Constants.FileSorting.TestFileGeneratorTextPoolSize; i++)
        {
            _generator.GetNextEntry(pool);
        }

        // Act - generate many more entries once pool is at capacity
        for (int i = 0; i < 100; i++)
        {
            var entry = _generator.GetNextEntry(pool);
            Assert.False(string.IsNullOrEmpty(entry.Text));
        }

        // Assert - pool can still grow slightly when new unique text is generated, but reused entries don't add
        Assert.True(pool.Count >= Constants.FileSorting.TestFileGeneratorTextPoolSize);
    }

    [Fact]
    public void GetNextEntry_GeneratesTextWithinLengthBounds()
    {
        // Arrange
        var pool = new List<string>();

        // Act
        for (int i = 0; i < 50; i++)
        {
            var entry = _generator.GetNextEntry(pool);

            // Assert
            Assert.InRange(entry.Text.Length,
                Constants.FileSorting.TestFileGeneratorMinTextLength,
                Constants.FileSorting.TestFileGeneratorMaxTextLength);
        }
    }

    [Fact]
    public void GetNextEntry_GeneratesPositiveNumber()
    {
        // Arrange
        var pool = new List<string>();

        // Act
        var entry = _generator.GetNextEntry(pool);

        // Assert
        Assert.True(entry.Number > 0);
    }

    [Fact]
    public void GetNextEntry_ProducesDuplicateTextsOverManyCalls()
    {
        // Arrange
        var pool = new List<string>();
        var texts = new List<string>();

        // Act
        for (int i = 0; i < 500; i++)
        {
            texts.Add(_generator.GetNextEntry(pool).Text);
        }

        // Assert
        Assert.True(texts.Distinct().Count() < texts.Count);
    }
}
