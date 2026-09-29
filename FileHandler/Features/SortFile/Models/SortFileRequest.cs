using System.Diagnostics.CodeAnalysis;

namespace FileHandler.Features.SortFile.Models;

[ExcludeFromCodeCoverage]
public class SortFileRequest
{
    /// <summary>
    /// Path to the file to be sorted.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}
