using FileHandler.Models;

namespace FileHandler.Features.GenerateTestFile.Services;

/// <summary>
/// Default implementation of <see cref="ILineEntryGenerator"/> that produces random line entries,
/// reusing a pooled set of texts at a fixed ratio to simulate realistic duplicate content.
/// </summary>
public class LineEntryGenerator : ILineEntryGenerator
{
    private const string TextCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    /// <inheritdoc />
    public LineEntry GetNextEntry(List<string> textPool)
    {
        string text = GetNextText(textPool);
        long number = Random.Shared.NextInt64(1, long.MaxValue);
        return new LineEntry(number, text);
    }

    private static string GetNextText(List<string> textPool)
    {
        if (textPool.Count < Constants.FileSorting.TestFileGeneratorTextPoolSize)
        {
            string newText = GenerateRandomText();
            textPool.Add(newText);
            return newText;
        }

        if (Random.Shared.NextDouble() < Constants.FileSorting.TestFileGeneratorDuplicateTextRatio)
        {
            return textPool[Random.Shared.Next(textPool.Count)];
        }

        return GenerateRandomText();
    }

    private static string GenerateRandomText()
    {
        int length = Random.Shared.Next(
            Constants.FileSorting.TestFileGeneratorMinTextLength,
            Constants.FileSorting.TestFileGeneratorMaxTextLength + 1);

        return string.Create(length, 0, (span, _) =>
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = TextCharacters[Random.Shared.Next(TextCharacters.Length)];
            }
        });
    }
}
