using FileHandler.Helpers;
using FileHandler.Models;
using System.Globalization;
using System.Text;

namespace FileHandler.Services;

/// <summary>
/// Performs a k-way merge of already-sorted chunk files into a single output file,
/// using a priority queue to always write the smallest remaining entry across all chunks.
/// </summary>
public class KWayChunkMerger : IChunkMerger
{
    private readonly IFileManager _fileSystem;
    private readonly IComparer<LineEntry> _lineComparer;

    public KWayChunkMerger(IFileManager fileSystem, IComparer<LineEntry> lineComparer)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _lineComparer = lineComparer ?? throw new ArgumentNullException(nameof(lineComparer));
    }

    /// <summary>
    /// Merges the given sorted chunk files directly into a single output file.
    /// </summary>
    public async Task MergeAsync(
        List<string> chunkFiles,
        string outputFilePath,
        CancellationToken cancellationToken)
    {
        if (chunkFiles.Count == 0)
        {
            _fileSystem.WriteAllText(outputFilePath, string.Empty);
            return;
        }

        if (chunkFiles.Count == 1)
        {
            _fileSystem.Copy(chunkFiles[0], outputFilePath, overwrite: true);
            return;
        }

        var readers = new StreamReader[chunkFiles.Count];

        try
        {
            var mergeHeap = await OpenReadersAndSeedHeapAsync(chunkFiles, readers, cancellationToken);

            using FileStream outputFileStream = _fileSystem.Create(outputFilePath);
            using var outputWriter = new StreamWriter(outputFileStream, Encoding.UTF8, bufferSize: Constants.FileSorting.FileWriterBufferSize);

            await WriteMergedEntriesAsync(mergeHeap, readers, outputWriter, cancellationToken);
        }
        finally
        {
            foreach (var reader in readers)
            {
                reader?.Dispose();
            }
        }
    }

    /// <summary>
    /// Opens a <see cref="StreamReader"/> for each chunk file and seeds the merge heap with the
    /// first entry read from each reader.
    /// </summary>
    private async Task<PriorityQueue<(LineEntry Entry, int ChunkIndex), LineEntry>> OpenReadersAndSeedHeapAsync(
        List<string> chunkFiles,
        StreamReader[] readers,
        CancellationToken cancellationToken)
    {
        var mergeHeap = new PriorityQueue<(LineEntry Entry, int ChunkIndex), LineEntry>(_lineComparer);

        for (int i = 0; i < chunkFiles.Count; i++)
        {
            FileStream? fileStream = null;
            try
            {
                fileStream = _fileSystem.OpenRead(chunkFiles[i]);
                readers[i] = new StreamReader(fileStream, Encoding.UTF8, bufferSize: Constants.FileSorting.FileReaderBufferSize);
            }
            catch
            {
                fileStream?.Dispose();
                throw;
            }

            if (await ReadNextLineAsync(readers[i]!, cancellationToken) is { } line)
            {
                mergeHeap.Enqueue((line, i), line);
            }
        }

        return mergeHeap;
    }

    /// <summary>
    /// Repeatedly dequeues the smallest entry from the merge heap, writes it to the output,
    /// and refills the heap from the same chunk reader until all entries are exhausted.
    /// </summary>
    private static async Task WriteMergedEntriesAsync(
        PriorityQueue<(LineEntry Entry, int ChunkIndex), LineEntry> mergeHeap,
        StreamReader[] readers,
        StreamWriter outputWriter,
        CancellationToken cancellationToken)
    {
        while (mergeHeap.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (entry, chunkIndex) = mergeHeap.Dequeue();
            WriteEntry(outputWriter, entry);

            if (await ReadNextLineAsync(readers[chunkIndex]!, cancellationToken) is { } nextLine)
            {
                mergeHeap.Enqueue((nextLine, chunkIndex), nextLine);
            }
        }
    }

    private static async Task<LineEntry?> ReadNextLineAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        string? line = await reader.ReadLineAsync(cancellationToken);
        return line != null && LineParser.TryParse(line, out var entry)
            ? entry
            : null;
    }

    /// <summary>
    /// Writes a single entry in the format "<Number>. <Text>" without allocating an
    /// intermediate formatted string for the number.
    /// </summary>
    private static void WriteEntry(StreamWriter writer, LineEntry entry)
    {
        Span<char> numberBuffer = stackalloc char[20];
        entry.Number.TryFormat(numberBuffer, out int charsWritten, provider: CultureInfo.InvariantCulture);

        writer.Write(numberBuffer[..charsWritten]);
        writer.Write(". ");
        writer.Write(entry.Text);
        writer.WriteLine();
    }
}
