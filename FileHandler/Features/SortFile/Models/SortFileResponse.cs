using System.Diagnostics.CodeAnalysis;

namespace FileHandler.Features.SortFile.Models;

[ExcludeFromCodeCoverage]
public class SortFileResponse
{
    /// <summary>
    /// Path to the sorted output file.
    /// </summary>
    public string SortedFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Number of lines processed in the file.
    /// </summary>
    public long LineCount { get; set; }
}
