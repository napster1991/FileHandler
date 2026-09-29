using FileHandler.Helpers;
using FileHandler.Models;

namespace FileHandler.Unit.Helpers;

public class LineParserTests
{
    [Fact]
    public void TryParse_ValidLine_ReturnsTrue()
    {
        // Arrange
        string line = "415. Apple";

        // Act
        bool result = LineParser.TryParse(line, out var entry);

        // Assert
        Assert.True(result);
        Assert.NotNull(entry);
        Assert.Equal(415, entry.Number);
        Assert.Equal("Apple", entry.Text);
    }

    [Fact]
    public void TryParse_ValidLineWithMultipleWords_ReturnsTrue()
    {
        // Arrange
        string line = "32. Cherry is the best";

        // Act
        bool result = LineParser.TryParse(line, out var entry);

        // Assert
        Assert.True(result);
        Assert.NotNull(entry);
        Assert.Equal(32, entry.Number);
        Assert.Equal("Cherry is the best", entry.Text);
    }

    [Fact]
    public void TryParse_LargeNumber_ReturnsTrue()
    {
        // Arrange
        string line = "30432. Something something something";

        // Act
        bool result = LineParser.TryParse(line, out var entry);

        // Assert
        Assert.True(result);
        Assert.NotNull(entry);
        Assert.Equal(30432, entry.Number);
        Assert.Equal("Something something something", entry.Text);
    }

    [Fact]
    public void TryParse_SingleDigitNumber_ReturnsTrue()
    {
        // Arrange
        string line = "1. Apple";

        // Act
        bool result = LineParser.TryParse(line, out var entry);

        // Assert
        Assert.True(result);
        Assert.NotNull(entry);
        Assert.Equal(1, entry.Number);
        Assert.Equal("Apple", entry.Text);
    }

    [Fact]
    public void TryParse_NullLine_ReturnsFalse()
    {
        // Act
        bool result = LineParser.TryParse(null, out var entry);

        // Assert
        Assert.False(result);
        Assert.Null(entry);
    }

    [Fact]
    public void TryParse_EmptyLine_ReturnsFalse()
    {
        // Act
        bool result = LineParser.TryParse(string.Empty, out var entry);

        // Assert
        Assert.False(result);
        Assert.Null(entry);
    }

    [Fact]
    public void TryParse_WhitespaceLine_ReturnsFalse()
    {
        // Act
        bool result = LineParser.TryParse("   ", out var entry);

        // Assert
        Assert.False(result);
        Assert.Null(entry);
    }

    [Fact]
    public void TryParse_MissingDot_ReturnsFalse()
    {
        // Arrange
        string line = "415 Apple";

        // Act
        bool result = LineParser.TryParse(line, out var entry);

        // Assert
        Assert.False(result);
        Assert.Null(entry);
    }

    [Fact]
    public void TryParse_InvalidNumber_ReturnsFalse()
    {
        // Arrange
        string line = "abc. Apple";

        // Act
        bool result = LineParser.TryParse(line, out var entry);

        // Assert
        Assert.False(result);
        Assert.Null(entry);
    }

    [Fact]
    public void TryParse_DotAtStart_ReturnsFalse()
    {
        // Arrange
        string line = ". Apple";

        // Act
        bool result = LineParser.TryParse(line, out var entry);

        // Assert
        Assert.False(result);
        Assert.Null(entry);
    }

    [Fact]
    public void TryParse_NoTextAfterDot_ReturnsFalse()
    {
        // Arrange
        string line = "415.";

        // Act
        bool result = LineParser.TryParse(line, out var entry);

        // Assert
        Assert.False(result);
        Assert.Null(entry);
    }

    [Fact]
    public void Parse_ValidLine_ReturnsEntry()
    {
        // Arrange
        string line = "415. Apple";

        // Act
        var entry = LineParser.Parse(line);

        // Assert
        Assert.Equal(415, entry.Number);
        Assert.Equal("Apple", entry.Text);
    }

    [Fact]
    public void Parse_InvalidLine_ThrowsFormatException()
    {
        // Arrange
        string line = "invalid line";

        // Act & Assert
        Assert.Throws<FormatException>(() => LineParser.Parse(line));
    }

    [Fact]
    public void ToString_ReturnsOriginalFormat()
    {
        // Arrange
        var entry = new LineEntry(415, "Apple");

        // Act
        string result = entry.ToString();

        // Assert
        Assert.Equal("415. Apple", result);
    }

    [Fact]
    public void TryParse_TextWithSpecialCharacters_ReturnsTrue()
    {
        // Arrange
        string line = "100. Special @#$% characters & symbols!";

        // Act
        bool result = LineParser.TryParse(line, out var entry);

        // Assert
        Assert.True(result);
        Assert.NotNull(entry);
        Assert.Equal("Special @#$% characters & symbols!", entry.Text);
    }

    [Fact]
    public void TryParse_TextWithLeadingSpaces_ReturnsTrue()
    {
        // Arrange
        string line = "100.   Leading spaces";

        // Act
        bool result = LineParser.TryParse(line, out var entry);

        // Assert
        Assert.True(result);
        Assert.NotNull(entry);
        Assert.Equal("  Leading spaces", entry.Text);
    }

    [Fact]
    public void TryParse_ZeroNumber_ReturnsTrue()
    {
        // Arrange
        string line = "0. Zero";

        // Act
        bool result = LineParser.TryParse(line, out var entry);

        // Assert
        Assert.True(result);
        Assert.NotNull(entry);
        Assert.Equal(0, entry.Number);
        Assert.Equal("Zero", entry.Text);
    }
}
