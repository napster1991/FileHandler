using FileHandler.Models;
using FileHandler.Services;

namespace FileHandler.Unit.Services;

public class LineEntryComparerTests
{
    private readonly LineEntryComparer _comparer = new();

    [Fact]
    public void Compare_DifferentText_SortsAlphabetically()
    {
        var a = new LineEntry(1, "Banana");
        var b = new LineEntry(1, "Apple");

        int result = _comparer.Compare(a, b);

        Assert.True(result > 0);
    }

    [Fact]
    public void Compare_SameText_SortsByNumber()
    {
        var a = new LineEntry(2, "Apple");
        var b = new LineEntry(1, "Apple");

        int result = _comparer.Compare(a, b);

        Assert.True(result > 0);
    }

    [Fact]
    public void Compare_EqualEntries_ReturnsZero()
    {
        var a = new LineEntry(1, "Apple");
        var b = new LineEntry(1, "Apple");

        int result = _comparer.Compare(a, b);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Compare_CaseSensitiveOrdinalComparison()
    {
        var lower = new LineEntry(1, "apple");
        var upper = new LineEntry(1, "Apple");

        int result = _comparer.Compare(upper, lower);

        Assert.True(result < 0);
    }
}
