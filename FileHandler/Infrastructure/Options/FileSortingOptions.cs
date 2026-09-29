using System.Diagnostics.CodeAnalysis;

namespace FileHandler.Infrastructure.Options;

[ExcludeFromCodeCoverage]
public sealed class FileSortingOptions
{
    public const string Key = "FileSorting";

    public string InputDirectory { get; set; }

    public string OutputDirectory { get; set; }
}
