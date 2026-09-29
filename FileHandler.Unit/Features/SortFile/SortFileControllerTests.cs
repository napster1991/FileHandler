using FileHandler.Features.SortFile;
using FileHandler.Features.SortFile.Models;
using FileHandler.Features.SortFile.Services;
using FileHandler.Infrastructure.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FileHandler.Unit.Features.SortFile;

public class SortFileControllerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly IFileSorter _fileSorter;
    private readonly IOptions<FileSortingOptions> _options;
    private readonly ILogger<SortFileController> _logger;
    private readonly SortFileController _controller;

    public SortFileControllerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        _fileSorter = Substitute.For<IFileSorter>();
        _options = Options.Create(new FileSortingOptions
        {
            InputDirectory = _testDirectory,
            OutputDirectory = _testDirectory
        });
        _logger = Substitute.For<ILogger<SortFileController>>();
        _controller = new SortFileController(_fileSorter, _options, _logger);
    }

    [Fact]
    public async Task Sort_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Sort(null!);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Request body is required.", badRequest.Value);
    }

    [Fact]
    public async Task Sort_EmptyFilePath_ReturnsBadRequest()
    {
        // Arrange
        var request = new SortFileRequest { FilePath = string.Empty };

        // Act
        var result = await _controller.Sort(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("FilePath is required.", badRequest.Value);
    }

    [Fact]
    public async Task Sort_WhitespaceFilePath_ReturnsBadRequest()
    {
        // Arrange
        var request = new SortFileRequest { FilePath = "   " };

        // Act
        var result = await _controller.Sort(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Sort_ValidRequest_ReturnsOkWithResponse()
    {
        // Arrange
        var request = new SortFileRequest { FilePath = "input.txt" };
        _fileSorter.SortFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(42L));

        // Act
        var result = await _controller.Sort(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SortFileResponse>(okResult.Value);
        Assert.Equal(42, response.LineCount);
        Assert.Contains("sorted_input.txt", response.SortedFilePath);
    }

    [Fact]
    public async Task Sort_ValidRequest_CreatesOutputDirectory()
    {
        // Arrange
        var request = new SortFileRequest { FilePath = "input.txt" };
        _fileSorter.SortFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0L));

        // Act
        await _controller.Sort(request);

        // Assert
        Assert.True(Directory.Exists(_testDirectory));
    }

    [Fact]
    public async Task Sort_ValidRequest_DelegatesToFileSorter()
    {
        // Arrange
        var request = new SortFileRequest { FilePath = "input.txt" };
        _fileSorter.SortFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(5L));

        // Act
        await _controller.Sort(request);

        // Assert
        await _fileSorter.Received(1).SortFileAsync(
            "input.txt", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SortFileController_NullFileSorter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SortFileController(null!, _options, _logger));
    }

    [Fact]
    public void SortFileController_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SortFileController(_fileSorter, null!, _logger));
    }

    [Fact]
    public void SortFileController_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SortFileController(_fileSorter, _options, null!));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
