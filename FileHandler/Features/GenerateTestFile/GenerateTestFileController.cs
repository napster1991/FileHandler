using FileHandler.Features.GenerateTestFile.Models;
using FileHandler.Features.GenerateTestFile.Services;
using FileHandler.Features.SortFile.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileHandler.Features.GenerateTestFile;

[ApiController]
[Produces("application/json")]
[Route("api/generateTestFile")]
public class GenerateTestFileController : ControllerBase
{
    private readonly ITestFileGenerator _testFileGenerator;
    private readonly ILogger<GenerateTestFileController> _logger;

    public GenerateTestFileController(
        ITestFileGenerator testFileGenerator,
        ILogger<GenerateTestFileController> logger)
    {
        _testFileGenerator = testFileGenerator ?? throw new ArgumentNullException(nameof(testFileGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a test file of approximately the requested size, with a fixed ratio of duplicate text values.
    /// </summary>
    /// <param name="request">Request containing the target file size in MB</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response containing the generated file path and line count</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(SortFileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GenerateTestFileResponse>> Generate(
        [FromBody] GenerateTestFileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest("Request body is required.");
        }

        if (request.FileSizeMb <= 0)
        {
            return BadRequest("FileSizeMb must be greater than zero.");
        }

        _logger.LogInformation("Received generate request for file size (MB): {FileSizeMb}", request.FileSizeMb);

        var result = await _testFileGenerator.GenerateFileAsync(request.FileSizeMb, cancellationToken);

        return Ok(new GenerateTestFileResponse
        {
            FilePath = result.FilePath,
            LineCount = result.LineCount
        });
    }
}
