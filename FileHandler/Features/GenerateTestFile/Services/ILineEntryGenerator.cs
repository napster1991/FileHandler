using FileHandler.Models;

namespace FileHandler.Features.GenerateTestFile.Services;

/// <summary>
/// Generates random <see cref="LineEntry"/> instances for test file generation.
/// </summary>
public interface ILineEntryGenerator
{
    /// <summary>
    /// Generates the next line entry, optionally reusing a pooled text value to simulate duplicate content.
    /// </summary>
    /// <param name="textPool">The pool of previously generated text values used to introduce duplicates.</param>
    /// <returns>A newly generated <see cref="LineEntry"/>.</returns>
    LineEntry GetNextEntry(List<string> textPool);
}
