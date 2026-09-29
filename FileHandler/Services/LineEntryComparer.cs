using FileHandler.Models;

namespace FileHandler.Services;

/// <summary>
/// Dedicated comparer for <see cref="LineEntry"/> that sorts by text (ascending) first,
/// then by number (ascending). Implemented as a concrete sealed class (rather than a
/// delegate-based <see cref="Comparer{T}.Create(Comparison{T})"/> instance) to avoid
/// delegate-invocation overhead in the sort's hot loop, since this is a JIT-friendlier code path.
/// </summary>
public sealed class LineEntryComparer : IComparer<LineEntry>
{
    public int Compare(LineEntry? x, LineEntry? y)
    {
        int textComparison = string.CompareOrdinal(x!.Text, y!.Text);
        return textComparison != 0 ? textComparison : x.Number.CompareTo(y.Number);
    }
}
