using System.Diagnostics.CodeAnalysis;

namespace FileHandler.Features.GenerateTestFile.Models;

[ExcludeFromCodeCoverage]
public class GenerateTestFileRequest
{
    /// <summary>
    /// The approximate target file size in megabytes. Must be greater than zero.
    /// </summary>
    public long FileSizeMb { get; set; }
}
