using FileHandler.Features.GenerateTestFile.Models;

namespace FileHandler.Features.GenerateTestFile.Services;

/// <summary>
/// Generates test files in the format used by the file sorting service, useful for testing and benchmarking.
/// </summary>
public interface ITestFileGenerator
{
    /// <summary>
    /// Generates a test file of approximately the requested size, containing lines in the format
    /// <![CDATA[<Number>. <Text>]]>. A fixed ratio of lines will share the same Text value to
    /// simulate realistic duplicate content.
    /// </summary>
    /// <param name="fileSizeMb">The approximate target file size in megabytes. Must be greater than zero.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result containing the generated file path and the number of lines written</returns>
    Task<TestFileGenerationResult> GenerateFileAsync(long fileSizeMb, CancellationToken cancellationToken = default);
}
