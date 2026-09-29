using FileHandler.Features.GenerateTestFile.Services;
using FileHandler.Helpers;
using FileHandler.Infrastructure.Options;
using FileHandler.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FileHandler.Unit.Features.GenerateTestFile.Services;

public class TestFileGeneratorTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ILogger<TestFileGenerator> _logger;
    private readonly IFileManager _fileSystem;
    private readonly IOptions<FileSortingOptions> _options;
    private readonly ILineEntryGenerator _lineEntryGenerator;

    public TestFileGeneratorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _logger = Substitute.For<ILogger<TestFileGenerator>>();
        _fileSystem = Substitute.For<IFileManager>();
        _options = Options.Create(new FileSortingOptions
        {
            InputDirectory = _testDirectory,
            OutputDirectory = _testDirectory
        });

        _fileSystem.GetTempPath().Returns(_testDirectory);
        _fileSystem.Combine(Arg.Any<string[]>()).Returns(callInfo =>
        {
            var paths = (string[])callInfo[0];
            return Path.Combine(paths);
        });
        _fileSystem.Create(Arg.Any<string>()).Returns(callInfo =>
        {
            var path = (string)callInfo[0];
            return File.Create(path);
        });
        _fileSystem.CreateDirectory(Arg.Any<string>());

        _lineEntryGenerator = new LineEntryGenerator();
    }

    [Fact]
    public async Task GenerateFileAsync_ApproximatesRequestedSize()
    {
        // Arrange
        var generator = new TestFileGenerator(_logger, _fileSystem, _options, _lineEntryGenerator);
        const long requestedSizeMb = 1;

        // Act
        var result = await generator.GenerateFileAsync(requestedSizeMb);

        // Assert
        Assert.True(File.Exists(result.FilePath));
        var fileInfo = new FileInfo(result.FilePath);
        long targetBytes = requestedSizeMb * 1024 * 1024;

        Assert.True(fileInfo.Length >= targetBytes, $"Expected file size >= {targetBytes} bytes, but was {fileInfo.Length} bytes.");
        // Allow some overshoot tolerance since the last line can push past the target
        Assert.True(fileInfo.Length < targetBytes * 1.01, $"Expected file size to be close to {targetBytes} bytes, but was {fileInfo.Length} bytes.");
    }

    [Fact]
    public async Task GenerateFileAsync_ProducesParsableLines()
    {
        // Arrange
        var generator = new TestFileGenerator(_logger, _fileSystem, _options, _lineEntryGenerator);

        // Act
        var result = await generator.GenerateFileAsync(1);

        // Assert
        var lines = await File.ReadAllLinesAsync(result.FilePath);
        Assert.Equal(result.LineCount, lines.Length);

        foreach (var line in lines)
        {
            Assert.True(LineParser.TryParse(line, out var entry), $"Line '{line}' should be parsable");
            Assert.NotNull(entry);
            Assert.True(entry.Number > 0);
            Assert.False(string.IsNullOrEmpty(entry.Text));
        }
    }

    [Fact]
    public async Task GenerateFileAsync_ProducesDuplicateTextValues()
    {
        // Arrange
        var generator = new TestFileGenerator(_logger, _fileSystem, _options, _lineEntryGenerator);

        // Act
        var result = await generator.GenerateFileAsync(1);

        // Assert
        var lines = await File.ReadAllLinesAsync(result.FilePath);
        var texts = lines
            .Select(line => LineParser.TryParse(line, out var entry) ? entry.Text : null)
            .Where(text => text != null)
            .ToList();

        int distinctTextCount = texts.Distinct().Count();

        Assert.True(distinctTextCount < texts.Count, "Expected some text values to be duplicated across lines.");
    }

    [Fact]
    public async Task GenerateFileAsync_ZeroFileSize_ThrowsArgumentException()
    {
        // Arrange
        var generator = new TestFileGenerator(_logger, _fileSystem, _options, _lineEntryGenerator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => generator.GenerateFileAsync(0));
    }

    [Fact]
    public async Task GenerateFileAsync_NegativeFileSize_ThrowsArgumentException()
    {
        // Arrange
        var generator = new TestFileGenerator(_logger, _fileSystem, _options, _lineEntryGenerator);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => generator.GenerateFileAsync(-1));
    }

    [Fact]
    public async Task GenerateFileAsync_ValidRequest_CreatesInputDirectory()
    {
        // Arrange
        var generator = new TestFileGenerator(_logger, _fileSystem, _options, _lineEntryGenerator);

        // Act
        await generator.GenerateFileAsync(1);

        // Assert
        _fileSystem.Received(1).CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task GenerateFileAsync_ValidRequest_CombinesInputDirectoryWithGeneratedFileName()
    {
        // Arrange
        var generator = new TestFileGenerator(_logger, _fileSystem, _options, _lineEntryGenerator);

        // Act
        var result = await generator.GenerateFileAsync(1);

        // Assert
        _fileSystem.Received(1).Combine(Arg.Is<string[]>(paths =>
            paths.Length == 2 &&
            paths[0] == _testDirectory &&
            paths[1].StartsWith("generated_") &&
            paths[1].EndsWith(".txt")));
        Assert.StartsWith(_testDirectory, result.FilePath);
    }

    [Fact]
    public async Task GenerateFileAsync_CancelledBeforeStart_ThrowsOperationCanceledException()
    {
        // Arrange
        var generator = new TestFileGenerator(_logger, _fileSystem, _options, _lineEntryGenerator);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => generator.GenerateFileAsync(1, cts.Token));
    }

    [Fact]
    public async Task GenerateFileAsync_FileCreationThrows_LogsErrorAndRethrows()
    {
        // Arrange
        var thrownException = new IOException("disk full");
        _fileSystem.Create(Arg.Any<string>()).Returns(_ => throw thrownException);
        var generator = new TestFileGenerator(_logger, _fileSystem, _options, _lineEntryGenerator);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<IOException>(() => generator.GenerateFileAsync(1));
        Assert.Same(thrownException, actualException);

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            thrownException,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void TestFileGenerator_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestFileGenerator(null!, _fileSystem, _options, _lineEntryGenerator));
    }

    [Fact]
    public void TestFileGenerator_NullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestFileGenerator(_logger, null!, _options, _lineEntryGenerator));
    }

    [Fact]
    public void TestFileGenerator_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestFileGenerator(_logger, _fileSystem, null!, _lineEntryGenerator));
    }

    [Fact]
    public void TestFileGenerator_NullLineEntryGenerator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestFileGenerator(_logger, _fileSystem, _options, null!));
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
