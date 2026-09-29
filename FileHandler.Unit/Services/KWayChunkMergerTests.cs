using FileHandler.Models;
using FileHandler.Services;
using NSubstitute;

namespace FileHandler.Unit.Services;

public class KWayChunkMergerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly IFileManager _fileManager;
    private readonly IComparer<LineEntry> _lineComparer;
    private readonly KWayChunkMerger _merger;

    public KWayChunkMergerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _fileManager = Substitute.For<IFileManager>();
        _fileManager.When(fs => fs.WriteAllText(Arg.Any<string>(), Arg.Any<string>()))
            .Do(callInfo => File.WriteAllText((string)callInfo[0], (string)callInfo[1]));
        _fileManager.When(fs => fs.Copy(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()))
            .Do(callInfo => File.Copy((string)callInfo[0], (string)callInfo[1], (bool)callInfo[2]));
        _fileManager.OpenRead(Arg.Any<string>())
            .Returns(callInfo => File.OpenRead((string)callInfo[0]));
        _fileManager.Create(Arg.Any<string>())
            .Returns(callInfo => File.Create((string)callInfo[0]));

        _lineComparer = new LineEntryComparer();

        _merger = new KWayChunkMerger(_fileManager, _lineComparer);
    }

    [Fact]
    public async Task MergeAsync_NoChunks_WritesEmptyOutputFile()
    {
        // Arrange
        string outputFile = Path.Combine(_testDirectory, "output.txt");

        // Act
        await _merger.MergeAsync([], outputFile, CancellationToken.None);

        // Assert
        Assert.True(File.Exists(outputFile));
        Assert.Equal(string.Empty, File.ReadAllText(outputFile));
    }

    [Fact]
    public async Task MergeAsync_SingleChunk_CopiesChunkToOutput()
    {
        // Arrange
        string chunk = Path.Combine(_testDirectory, "chunk1.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        File.WriteAllLines(chunk, ["1. Apple", "2. Banana"]);

        // Act
        await _merger.MergeAsync([chunk], outputFile, CancellationToken.None);

        // Assert
        var lines = await File.ReadAllLinesAsync(outputFile);
        Assert.Equal(["1. Apple", "2. Banana"], lines);
    }

    [Fact]
    public async Task MergeAsync_MultipleSortedChunks_MergesInOrder()
    {
        // Arrange
        string chunk1 = Path.Combine(_testDirectory, "chunk1.txt");
        string chunk2 = Path.Combine(_testDirectory, "chunk2.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");

        File.WriteAllLines(chunk1, ["1. Apple", "3. Cherry"]);
        File.WriteAllLines(chunk2, ["2. Banana", "4. Date"]);

        // Act
        await _merger.MergeAsync([chunk1, chunk2], outputFile, CancellationToken.None);

        // Assert
        var lines = await File.ReadAllLinesAsync(outputFile);
        Assert.Equal(["1. Apple", "2. Banana", "3. Cherry", "4. Date"], lines);
    }

    [Fact]
    public async Task MergeAsync_ChunksWithDifferentLineCounts_MergesAllEntries()
    {
        // Arrange
        string chunk1 = Path.Combine(_testDirectory, "chunk1.txt");
        string chunk2 = Path.Combine(_testDirectory, "chunk2.txt");
        string chunk3 = Path.Combine(_testDirectory, "chunk3.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");

        File.WriteAllLines(chunk1, ["1. Apple"]);
        File.WriteAllLines(chunk2, ["2. Banana", "3. Cherry", "4. Date"]);
        File.WriteAllLines(chunk3, ["5. Elderberry"]);

        // Act
        await _merger.MergeAsync([chunk1, chunk2, chunk3], outputFile, CancellationToken.None);

        // Assert
        var lines = await File.ReadAllLinesAsync(outputFile);
        Assert.Equal(5, lines.Length);
        Assert.Equal(["1. Apple", "2. Banana", "3. Cherry", "4. Date", "5. Elderberry"], lines);
    }

    [Fact]
    public async Task MergeAsync_Cancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        string chunk1 = Path.Combine(_testDirectory, "chunk1.txt");
        string chunk2 = Path.Combine(_testDirectory, "chunk2.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        File.WriteAllLines(chunk1, ["1. Apple"]);
        File.WriteAllLines(chunk2, ["2. Banana"]);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _merger.MergeAsync([chunk1, chunk2], outputFile, cts.Token));
    }

    [Fact]
    public void KWayChunkMerger_NullFileSystem_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new KWayChunkMerger(null!, _lineComparer));
    }

    [Fact]
    public void KWayChunkMerger_NullLineComparer_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new KWayChunkMerger(_fileManager, null!));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
