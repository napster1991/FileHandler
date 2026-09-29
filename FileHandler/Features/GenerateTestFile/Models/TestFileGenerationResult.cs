using System.Diagnostics.CodeAnalysis;

namespace FileHandler.Features.GenerateTestFile.Models;

[ExcludeFromCodeCoverage]
/// <summary>
/// Represents the result of a test file generation operation.
/// </summary>
public record TestFileGenerationResult(string FilePath, long LineCount);
