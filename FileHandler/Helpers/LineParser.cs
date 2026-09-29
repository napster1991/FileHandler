using FileHandler.Models;
using System.Globalization;

namespace FileHandler.Helpers;

/// <summary>
/// Parses lines in the format: <Number>. <String>
/// </summary>
public static class LineParser
{
    private const char Dot = '.';
    private const char Space = ' ';

    /// <summary>
    /// Attempts to parse a line into a LineEntry.
    /// Expected format: <Number>. <String>
    /// </summary>
    /// <param name="line">The line to parse</param>
    /// <param name="entry">The parsed entry if successful</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    public static bool TryParse(string line, out LineEntry? entry)
    {
        entry = null;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        // Find the first dot followed by a space
        int dotIndex = line.IndexOf(Dot);
        if (dotIndex <= 0)
        {
            return false;
        }

        // Extract number part
        ReadOnlySpan<char> numberPart = line.AsSpan(0, dotIndex);
        if (!long.TryParse(numberPart, NumberStyles.None, CultureInfo.InvariantCulture, out long number))
        {
            return false;
        }

        // Extract text part (after dot and space)
        int textStartIndex = dotIndex + 1;
        if (textStartIndex >= line.Length)
        {
            return false;
        }

        // Skip the space if present
        if (line[textStartIndex] == Space)
        {
            textStartIndex++;
        }

        if (textStartIndex > line.Length)
        {
            return false;
        }

        string text = line.Substring(textStartIndex);
        entry = new LineEntry(number, text);
        return true;
    }

    /// <summary>
    /// Parses a line into a LineEntry, throwing an exception if parsing fails.
    /// </summary>
    public static LineEntry Parse(string line)
    {
        if (!TryParse(line, out var entry) || entry is null)
        {
            throw new FormatException($"Invalid line format: '{line}'. Expected format: '<Number>. <String>'");
        }

        return entry;
    }
}
