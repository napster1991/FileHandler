using System.Diagnostics.CodeAnalysis;

namespace FileHandler.Features.GenerateTestFile.Models;

[ExcludeFromCodeCoverage]
public class GenerateTestFileResponse
{
    /// <summary>
    /// Path to the generated test file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Number of lines written to the file.
    /// </summary>
    public long LineCount { get; set; }
}
