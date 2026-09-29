using System.Diagnostics.CodeAnalysis;

namespace FileHandler.Models;

/// <summary>
/// Represents a parsed line entry from the input file with format: <Number>. <String>
/// </summary>
[ExcludeFromCodeCoverage]
public record LineEntry(long Number, string Text)
{
    /// <summary>
    /// Formats the entry back to the original line format: <Number>. <Text>
    /// </summary>
    public override string ToString() => $"{Number}. {Text}";
}
