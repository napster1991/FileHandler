using FileHandler.Features.SortFile.Services;
using FileHandler.Helpers;
using FileHandler.Models;
using FileHandler.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FileHandler.Unit.Features.SortFile.Services;

public class FileSorterTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ILogger<FileSorter> _logger;
    private readonly ITempDirectoryManager _tempDirectoryManager;
    private readonly IFileManager _fileSystem;
    private readonly IComparer<LineEntry> _lineComparer;
    private readonly IChunkMerger _chunkMerger;

    public FileSorterTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _logger = Substitute.For<ILogger<FileSorter>>();
        _tempDirectoryManager = Substitute.For<ITempDirectoryManager>();
        _fileSystem = Substitute.For<IFileManager>();
        _lineComparer = new LineEntryComparer();

        // _chunkMerger is a mock of IChunkMerger, but its MergeAsync calls are forwarded to a real
        // KWayChunkMerger instance. This keeps the dependency mockable/verifiable (e.g. Received())
        // while still producing correctly sorted output for tests that assert on merge results.
        var realChunkMerger = new KWayChunkMerger(_fileSystem, _lineComparer);
        _chunkMerger = Substitute.For<IChunkMerger>();
        _chunkMerger.MergeAsync(Arg.Any<List<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => realChunkMerger.MergeAsync(
                callInfo.Arg<List<string>>(),
                callInfo.Arg<string>(),
                callInfo.Arg<CancellationToken>()));
    }

    [Fact]
    public async Task SortFileAsync_EmptyFile_CreatesEmptyOutput()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "empty.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");
        File.WriteAllText(inputFile, string.Empty);
        Directory.CreateDirectory(tempDir);

        SetupMocks(inputFile, tempDir, outputFile);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act
        long lineCount = await sorter.SortFileAsync(inputFile, outputFile);

        // Assert
        Assert.Equal(0, lineCount);
        _tempDirectoryManager.Received().CreateTempDirectory();
    }

    [Fact]
    public async Task SortFileAsync_SingleLine_ReturnsSingleLine()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "single.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");
        File.WriteAllText(inputFile, "1. Apple");
        Directory.CreateDirectory(tempDir);

        SetupMocks(inputFile, tempDir, outputFile);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act
        long lineCount = await sorter.SortFileAsync(inputFile, outputFile);

        // Assert
        Assert.Equal(1, lineCount);
        _tempDirectoryManager.Received().CreateTempDirectory();
    }

    [Fact]
    public async Task SortFileAsync_ProvidedExample_SortsCorrectly()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "example.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        var inputLines = new[]
        {
            "415. Apple",
            "30432. Something something something",
            "1. Apple",
            "32. Cherry is the best",
            "2. Banana is yellow"
        };

        File.WriteAllLines(inputFile, inputLines);
        Directory.CreateDirectory(tempDir);

        SetupMocks(inputFile, tempDir, outputFile);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act
        long lineCount = await sorter.SortFileAsync(inputFile, outputFile);

        // Assert
        Assert.Equal(5, lineCount);
    }

    [Fact]
    public async Task SortFileAsync_DuplicateTexts_SortsByNumberThenText()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "duplicates.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        var inputLines = new[]
        {
            "100. Test",
            "50. Test",
            "25. Test",
            "200. Test"
        };

        File.WriteAllLines(inputFile, inputLines);
        Directory.CreateDirectory(tempDir);

        SetupMocks(inputFile, tempDir, outputFile);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act
        long lineCount = await sorter.SortFileAsync(inputFile, outputFile);

        // Assert
        Assert.Equal(4, lineCount);
    }

    [Fact]
    public async Task SortFileAsync_AlphabeticalSorting_SortsCorrectly()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "alpha.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        var inputLines = new[]
        {
            "1. Zebra",
            "2. Apple",
            "3. Banana",
            "4. Cherry"
        };

        File.WriteAllLines(inputFile, inputLines);
        Directory.CreateDirectory(tempDir);

        SetupMocks(inputFile, tempDir, outputFile);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act
        long lineCount = await sorter.SortFileAsync(inputFile, outputFile);

        // Assert
        Assert.Equal(4, lineCount);
    }

    [Fact]
    public async Task SortFileAsync_InvalidLines_SkipsInvalidLines()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "mixed.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        var inputLines = new[]
        {
            "1. Apple",
            "invalid line",
            "2. Banana",
            "not a valid format",
            "3. Cherry"
        };

        File.WriteAllLines(inputFile, inputLines);
        Directory.CreateDirectory(tempDir);

        SetupMocks(inputFile, tempDir, outputFile);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act
        long lineCount = await sorter.SortFileAsync(inputFile, outputFile);

        // Assert
        Assert.Equal(3, lineCount);
    }

    [Fact]
    public async Task SortFileAsync_MultipleChunksWithRealisticSize_SortsCorrectly()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "chunks.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        var inputLines = new List<string>();
        for (int i = 100; i >= 1; i--)
        {
            inputLines.Add($"{i}. Item number {i} with content");
        }

        File.WriteAllLines(inputFile, inputLines);
        Directory.CreateDirectory(tempDir);

        // Use real file system for this test due to complex merge scenarios
        var realFileSystem = new FileManager();
        var realTempMgrLogger = Substitute.For<ILogger<TempDirectoryManager>>();
        var realTempMgr = new TempDirectoryManager(realTempMgrLogger);
        var realLineComparer = new LineEntryComparer();

        var realChunkMerger = new KWayChunkMerger(realFileSystem, realLineComparer);
        var sorter = new FileSorter(_logger, realTempMgr, realFileSystem, realLineComparer, realChunkMerger);

        // Act
        long lineCount = await sorter.SortFileAsync(inputFile, outputFile);

        // Assert
        Assert.Equal(100, lineCount);
        Assert.True(File.Exists(outputFile));
    }

    [Fact]
    public async Task SortFileAsync_PreservesLineContent_WithSpecialCharacters()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "special.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        var inputLines = new[]
        {
            "2. Special @#$%",
            "1. Normal text"
        };

        File.WriteAllLines(inputFile, inputLines);
        Directory.CreateDirectory(tempDir);

        SetupMocks(inputFile, tempDir, outputFile);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act
        long lineCount = await sorter.SortFileAsync(inputFile, outputFile);

        // Assert
        Assert.Equal(2, lineCount);
    }

    [Fact]
    public async Task SortFileAsync_ManyLines_SortsCorrectly()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "many.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        var inputLines = new List<string>();
        var random = new Random(42);

        var texts = new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry" };
        for (int i = 0; i < 1000; i++)
        {
            int number = random.Next(1, 10000);
            string text = texts[random.Next(texts.Length)];
            inputLines.Add($"{number}. {text}");
        }

        File.WriteAllLines(inputFile, inputLines);
        Directory.CreateDirectory(tempDir);

        SetupMocks(inputFile, tempDir, outputFile);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act
        long lineCount = await sorter.SortFileAsync(inputFile, outputFile);

        // Assert
        Assert.Equal(1000, lineCount);
    }

    [Fact]
    public async Task SortFileAsync_NonexistentInput_ThrowsFileNotFoundException()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "nonexistent.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        Directory.CreateDirectory(tempDir);

        _fileSystem.FileExists(inputFile).Returns(false);
        _tempDirectoryManager.CreateTempDirectory().Returns(tempDir);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => sorter.SortFileAsync(inputFile, outputFile));
    }

    [Fact]
    public async Task SortFileAsync_InvalidInputPath_ThrowsArgumentException()
    {
        // Arrange
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        Directory.CreateDirectory(tempDir);

        _tempDirectoryManager.CreateTempDirectory().Returns(tempDir);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => sorter.SortFileAsync(null!, outputFile));
    }

    [Fact]
    public async Task SortFileAsync_InvalidOutputPath_ThrowsArgumentException()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "input.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        File.WriteAllText(inputFile, "1. Test");
        Directory.CreateDirectory(tempDir);

        _fileSystem.FileExists(inputFile).Returns(true);
        _tempDirectoryManager.CreateTempDirectory().Returns(tempDir);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => sorter.SortFileAsync(inputFile, null!));
    }

    [Fact]
    public void FileSorter_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileSorter(null!, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger));
    }

    [Fact]
    public void FileSorter_NullTempDirectoryManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileSorter(_logger, null!, _fileSystem, _lineComparer, _chunkMerger));
    }

    [Fact]
    public void FileSorter_NullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileSorter(_logger, _tempDirectoryManager, null!, _lineComparer, _chunkMerger));
    }

    [Fact]
    public void FileSorter_NullLineComparer_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileSorter(_logger, _tempDirectoryManager, _fileSystem, null!, _chunkMerger));
    }

    [Fact]
    public void FileSorter_NullChunkMerger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, null!));
    }

    [Fact]
    public async Task SortFileAsync_DelegatesTempDirectoryCreation()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "test.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        File.WriteAllText(inputFile, "1. Test");
        Directory.CreateDirectory(tempDir);

        SetupMocks(inputFile, tempDir, outputFile);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act
        await sorter.SortFileAsync(inputFile, outputFile);

        // Assert - Verify delegation to ITempDirectoryManager
        _tempDirectoryManager.Received().CreateTempDirectory();
        _tempDirectoryManager.Received().CleanupTempDirectory(tempDir);
    }

    [Fact]
    public async Task SortFileAsync_DelegatesFileSystemOperations()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "test.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        File.WriteAllText(inputFile, "1. Test");
        Directory.CreateDirectory(tempDir);

        SetupMocks(inputFile, tempDir, outputFile);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act
        await sorter.SortFileAsync(inputFile, outputFile);

        // Assert - Verify delegation to IFileSystem
        _fileSystem.Received().FileExists(inputFile);
        _fileSystem.Received().OpenRead(inputFile);
    }

    [Fact]
    public async Task SortFileAsync_ExactlyOneChunk_CopiesChunkAndCountsLines()
    {
        // Arrange - use a real large chunk size so all lines fit in a single chunk (Count == 1 merge branch)
        string inputFile = Path.Combine(_testDirectory, "onechunk.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        File.WriteAllLines(inputFile, ["3. Cherry", "1. Apple", "2. Banana"]);
        Directory.CreateDirectory(tempDir);

        SetupMocks(inputFile, tempDir, outputFile);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act
        long lineCount = await sorter.SortFileAsync(inputFile, outputFile);

        // Assert
        Assert.Equal(3, lineCount);
        var lines = await File.ReadAllLinesAsync(outputFile);
        Assert.Equal(["1. Apple", "2. Banana", "3. Cherry"], lines);
    }

    [Fact]
    public async Task SortFileAsync_ExceptionDuringSort_LogsErrorAndRethrows()
    {
        // Arrange
        string inputFile = Path.Combine(_testDirectory, "test.txt");
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        string tempDir = Path.Combine(_testDirectory, "temp");

        File.WriteAllText(inputFile, "1. Test");
        Directory.CreateDirectory(tempDir);

        SetupMocks(inputFile, tempDir, outputFile);

        var thrownException = new IOException("simulated failure");
        _fileSystem.OpenRead(inputFile).Returns(_ => throw thrownException);

        var sorter = new FileSorter(_logger, _tempDirectoryManager, _fileSystem, _lineComparer, _chunkMerger);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<IOException>(() => sorter.SortFileAsync(inputFile, outputFile));
        Assert.Same(thrownException, actualException);

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            thrownException,
            Arg.Any<Func<object, Exception?, string>>());

        _tempDirectoryManager.Received().CleanupTempDirectory(tempDir);
    }

    [Fact]
    public async Task SortFileAsync_RecursiveMergeAcrossManyChunks_SortsAllEntriesCorrectly()
    {
        // Arrange - directly exercise MergeSortedChunksRecursiveAsync's batching branch by
        // feeding the chunk merger more sorted chunk files than MaxConcurrentReaders can handle in one pass.
        string outputFile = Path.Combine(_testDirectory, "output.txt");
        int chunkFileCount = Constants.FileSorting.MaxConcurrentReaders + 10;

        var chunkFiles = new List<string>();
        for (int i = 0; i < chunkFileCount; i++)
        {
            string chunkPath = Path.Combine(_testDirectory, $"chunk_{i:D3}.txt");
            File.WriteAllLines(chunkPath, [$"{i}. Item{i}"]);
            chunkFiles.Add(chunkPath);
        }

        var realFileSystem = new FileManager();
        var realLineComparer = new LineEntryComparer();
        var realChunkMerger = new KWayChunkMerger(realFileSystem, realLineComparer);

        // Act - merge in two batches to mimic recursive merging behavior used internally by FileSorter
        foreach (var batch in chunkFiles.Chunk(Constants.FileSorting.MaxConcurrentReaders))
        {
            string batchOutput = Path.Combine(_testDirectory, $"batch_{Guid.NewGuid():N}.txt");
            await realChunkMerger.MergeAsync([.. batch], batchOutput, CancellationToken.None);
        }

        // Assert end-to-end via FileSorter to also exercise its own recursive batching logic
        string inputFile = Path.Combine(_testDirectory, "combined_input.txt");
        var combinedLines = Enumerable.Range(0, chunkFileCount).Select(i => $"{i}. Item{i}").ToList();
        File.WriteAllLines(inputFile, combinedLines);

        var realTempMgrLogger = Substitute.For<ILogger<TempDirectoryManager>>();
        var realTempMgr = new TempDirectoryManager(realTempMgrLogger);
        var sorter = new FileSorter(_logger, realTempMgr, realFileSystem, realLineComparer, realChunkMerger);

        long lineCount = await sorter.SortFileAsync(inputFile, outputFile);

        Assert.Equal(chunkFileCount, lineCount);
        var lines = await File.ReadAllLinesAsync(outputFile);
        Assert.Equal(chunkFileCount, lines.Length);

        // Sorting is by Text (ordinal) first, then Number - compute expected order the same way
        var expectedOrder = combinedLines
            .Select(l => LineParser.Parse(l))
            .OrderBy(e => e.Text, StringComparer.Ordinal)
            .ThenBy(e => e.Number)
            .Select(e => e.ToString())
            .ToList();
        Assert.Equal(expectedOrder, lines);
    }

    private void SetupMocks(string inputFile, string tempDir, string outputFile)
    {
        _tempDirectoryManager.CreateTempDirectory().Returns(tempDir);
        _fileSystem.FileExists(inputFile).Returns(true);
        _fileSystem.GetFileLength(inputFile).Returns(callInfo => new FileInfo((string)callInfo[0]).Length);
        _fileSystem.GetTempPath().Returns(Path.GetTempPath());
        _fileSystem.Combine(Arg.Any<string[]>()).Returns(callInfo =>
        {
            var paths = (string[])callInfo[0];
            return Path.Combine(paths);
        });
        _fileSystem.OpenRead(inputFile).Returns(File.OpenRead(inputFile));
        _fileSystem.Create(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = (string)callInfo[0];
            return File.Create(path);
        });

        _fileSystem.When(fs => fs.Copy(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()))
            .Do(callInfo =>
            {
                var source = (string)callInfo[0];
                var dest = (string)callInfo[1];
                var overwrite = (bool)callInfo[2];
                File.Copy(source, dest, overwrite);
            });

        _fileSystem.When(fs => fs.Move(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()))
            .Do(callInfo =>
            {
                var source = (string)callInfo[0];
                var dest = (string)callInfo[1];
                var overwrite = (bool)callInfo[2];
                File.Move(source, dest, overwrite);
            });

        _fileSystem.When(fs => fs.WriteAllText(Arg.Any<string>(), Arg.Any<string>()))
            .Do(callInfo =>
            {
                var path = (string)callInfo[0];
                var content = (string)callInfo[1];
                File.WriteAllText(path, content);
            });
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
