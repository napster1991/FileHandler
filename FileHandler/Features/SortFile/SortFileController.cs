using FileHandler.Features.SortFile.Models;
using FileHandler.Features.SortFile.Services;
using FileHandler.Infrastructure.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FileHandler.Features.SortFile;

[ApiController]
[Produces("application/json")]
[Route("api/fileSorting")]
public class SortFileController : ControllerBase
{
    private readonly IFileSorter _fileSorter;
    private readonly IOptions<FileSortingOptions> _options;
    private readonly ILogger<SortFileController> _logger;

    public SortFileController(
        IFileSorter fileSorter,
        IOptions<FileSortingOptions> options,
        ILogger<SortFileController> logger)
    {
        _fileSorter = fileSorter ?? throw new ArgumentNullException(nameof(fileSorter));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sorts a file by text (ascending), then by number (ascending).
    /// </summary>
    /// <param name="request">Request containing the file path to sort</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response containing the sorted file path and line count</returns>
    [HttpPost("sort")]
    [ProducesResponseType(typeof(SortFileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SortFileResponse>> Sort(
        [FromBody] SortFileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            return BadRequest("FilePath is required.");
        }

        _logger.LogInformation("Received sort request for file path: {FilePath}", request.FilePath);

        string outputDirectory = _options.Value.OutputDirectory;
        Directory.CreateDirectory(outputDirectory);

        string outputPath = Path.Combine(
            outputDirectory,
            $"sorted_{Path.GetFileNameWithoutExtension(request.FilePath)}.txt");

        long lineCount = await _fileSorter.SortFileAsync(
            request.FilePath,
            outputPath,
            cancellationToken);

        return Ok(new SortFileResponse
        {
            SortedFilePath = outputPath,
            LineCount = lineCount
        });
    }
}
