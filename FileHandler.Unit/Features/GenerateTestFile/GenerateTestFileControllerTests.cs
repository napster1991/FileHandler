using FileHandler.Features.GenerateTestFile;
using FileHandler.Features.GenerateTestFile.Models;
using FileHandler.Features.GenerateTestFile.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FileHandler.Unit.Features.GenerateTestFile;

public class GenerateTestFileControllerTests
{
    private readonly ITestFileGenerator _testFileGenerator;
    private readonly ILogger<GenerateTestFileController> _logger;
    private readonly GenerateTestFileController _controller;

    public GenerateTestFileControllerTests()
    {
        _testFileGenerator = Substitute.For<ITestFileGenerator>();
        _logger = Substitute.For<ILogger<GenerateTestFileController>>();
        _controller = new GenerateTestFileController(_testFileGenerator, _logger);
    }

    [Fact]
    public async Task Generate_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Generate(null!);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Request body is required.", badRequest.Value);
    }

    [Fact]
    public async Task Generate_ZeroFileSize_ReturnsBadRequest()
    {
        // Arrange
        var request = new GenerateTestFileRequest { FileSizeMb = 0 };

        // Act
        var result = await _controller.Generate(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("FileSizeMb must be greater than zero.", badRequest.Value);
    }

    [Fact]
    public async Task Generate_NegativeFileSize_ReturnsBadRequest()
    {
        // Arrange
        var request = new GenerateTestFileRequest { FileSizeMb = -1 };

        // Act
        var result = await _controller.Generate(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Generate_ValidRequest_ReturnsOkWithResponse()
    {
        // Arrange
        var request = new GenerateTestFileRequest { FileSizeMb = 5 };
        _testFileGenerator.GenerateFileAsync(5, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TestFileGenerationResult("path/to/file.txt", 123)));

        // Act
        var result = await _controller.Generate(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<GenerateTestFileResponse>(okResult.Value);
        Assert.Equal("path/to/file.txt", response.FilePath);
        Assert.Equal(123, response.LineCount);
    }

    [Fact]
    public async Task Generate_ValidRequest_DelegatesToTestFileGenerator()
    {
        // Arrange
        var request = new GenerateTestFileRequest { FileSizeMb = 10 };
        _testFileGenerator.GenerateFileAsync(10, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TestFileGenerationResult("file.txt", 1)));

        // Act
        await _controller.Generate(request);

        // Assert
        await _testFileGenerator.Received(1).GenerateFileAsync(10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GenerateTestFileController_NullTestFileGenerator_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GenerateTestFileController(null!, _logger));
    }

    [Fact]
    public void GenerateTestFileController_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GenerateTestFileController(_testFileGenerator, null!));
    }
}
