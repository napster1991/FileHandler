using FileHandler.Features.GenerateTestFile.Models;
using FileHandler.Infrastructure.Options;
using FileHandler.Models;
using FileHandler.Services;
using Microsoft.Extensions.Options;
using System.Text;

namespace FileHandler.Features.GenerateTestFile.Services;

/// <summary>
/// Generates test files in the <![CDATA[<Number>. <Text>]]> format used by <see cref="SortFile.Services.IFileSorter"/>.
/// A fixed ratio of lines reuse a pooled set of texts to simulate realistic duplicate content.
/// </summary>
public class TestFileGenerator : ITestFileGenerator
{
    private readonly ILogger<TestFileGenerator> _logger;
    private readonly IFileManager _fileSystem;
    private readonly IOptions<FileSortingOptions> _options;
    private readonly ILineEntryGenerator _lineEntryGenerator;

    public TestFileGenerator(
        ILogger<TestFileGenerator> logger,
        IFileManager fileSystem,
        IOptions<FileSortingOptions> options,
        ILineEntryGenerator lineEntryGenerator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _lineEntryGenerator = lineEntryGenerator ?? throw new ArgumentNullException(nameof(lineEntryGenerator));
    }

    /// <summary>
    /// Generates a test file of approximately the requested size.
    /// </summary>
    /// <param name="fileSizeMb">The approximate target file size in megabytes. Must be greater than zero.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result containing the generated file path and the number of lines written</returns>
    public async Task<TestFileGenerationResult> GenerateFileAsync(long fileSizeMb, CancellationToken cancellationToken = default)
    {
        if (fileSizeMb <= 0)
        {
            throw new ArgumentException("File size must be greater than zero", nameof(fileSizeMb));
        }

        long targetBytes = fileSizeMb * Constants.FileSorting.BytesPerMegabyte;

        string inputDirectory = _options.Value.InputDirectory;
        _fileSystem.CreateDirectory(inputDirectory);

        string filePath = GenerateFilePath(inputDirectory);

        try
        {
            long lineCount = await GenerateAndWriteLinesAsync(filePath, targetBytes, cancellationToken);
            return new TestFileGenerationResult(filePath, lineCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while generating the test file.");
            throw;
        }
    }

    /// <summary>
    /// Generates a unique file path for the test file within the given directory.
    /// </summary>
    private string GenerateFilePath(string inputDirectory) =>
        _fileSystem.Combine(inputDirectory, $"generated_{Guid.NewGuid():N}.txt");

    /// <summary>
    /// Generates line entries and writes them to the target file until the target byte size is reached,
    /// returning the number of lines written.
    /// </summary>
    /// <remarks>
    /// The <paramref name="targetBytes"/> comparison uses the UTF-8 byte count of each line
    /// (via <see cref="Encoding.UTF8.GetByteCount(string)"/>) rather than character count, since
    /// generated text may include non-ASCII characters. The pooled text list passed to
    /// <see cref="ILineEntryGenerator.GetNextEntry"/> is pre-sized to
    /// <see cref="Constants.FileSorting.TestFileGeneratorTextPoolSize"/> as an initial capacity hint only;
    /// the generator is responsible for not growing it beyond that size.
    /// </remarks>
    private async Task<long> GenerateAndWriteLinesAsync(string filePath, long targetBytes, CancellationToken cancellationToken)
    {
        var textPool = new List<string>(Constants.FileSorting.TestFileGeneratorTextPoolSize);
        long currentBytes = 0;
        long lineCount = 0;

        using FileStream fileStream = _fileSystem.Create(filePath);
        using var writer = new StreamWriter(fileStream, Encoding.UTF8, bufferSize: Constants.FileSorting.FileWriterBufferSize * 4);

        while (currentBytes < targetBytes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LineEntry entry = _lineEntryGenerator.GetNextEntry(textPool);
            string line = entry.ToString();

            await writer.WriteLineAsync(line.AsMemory(), cancellationToken);

            currentBytes += Encoding.UTF8.GetByteCount(line) + Encoding.UTF8.GetByteCount(Environment.NewLine);
            lineCount++;
        }

        return lineCount;
    }
}
