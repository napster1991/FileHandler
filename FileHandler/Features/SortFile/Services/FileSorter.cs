using FileHandler.Helpers;
using FileHandler.Models;
using FileHandler.Services;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace FileHandler.Features.SortFile.Services;

/// <summary>
/// Memory-optimized file sorter for large files using external sorting with multi-threaded processing.
/// Prioritizes memory efficiency through bounded chunk queues and controlled concurrent reader limits.
/// Sorts lines by text (ascending), then by number (ascending).
/// </summary>
public class FileSorter : IFileSorter
{
    private readonly ILogger<FileSorter> _logger;
    private readonly ITempDirectoryManager _tempDirectoryManager;
    private readonly IFileManager _fileSystem;
    private readonly IComparer<LineEntry> _lineComparer;
    private readonly IChunkMerger _chunkMerger;

    public FileSorter(
        ILogger<FileSorter> logger,
        ITempDirectoryManager tempDirectoryManager,
        IFileManager fileSystem,
        IComparer<LineEntry> lineComparer,
        IChunkMerger chunkMerger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tempDirectoryManager = tempDirectoryManager ?? throw new ArgumentNullException(nameof(tempDirectoryManager));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _lineComparer = lineComparer ?? throw new ArgumentNullException(nameof(lineComparer));
        _chunkMerger = chunkMerger ?? throw new ArgumentNullException(nameof(chunkMerger));
    }

    /// <summary>
    /// Sorts a large file using external sorting with memory optimization.
    /// The chunk size is calculated automatically based on the input file's size.
    /// </summary>
    /// <param name="inputFilePath">Path to the input file</param>
    /// <param name="outputFilePath">Path to the output file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of lines processed</returns>
    public async Task<long> SortFileAsync(
        string inputFilePath,
        string outputFilePath,
        CancellationToken cancellationToken = default)
    {
        ValidateInputs(inputFilePath, outputFilePath);

        long maxChunkSizeBytes = ChunkSizeCalculator.CalculateChunkSizeBytes(_fileSystem.GetFileLength(inputFilePath));

        string tempDirectory = _tempDirectoryManager.CreateTempDirectory();

        try
        {
            var sortedChunkFiles = await SortChunksAsync(inputFilePath, tempDirectory, maxChunkSizeBytes, cancellationToken);
            long lineCount = await MergeSortedChunksAsync(sortedChunkFiles, outputFilePath, cancellationToken);
            return lineCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sorting the file.");
            throw;
        }
        finally
        {
            _tempDirectoryManager.CleanupTempDirectory(tempDirectory);
        }
    }

    /// <summary>
    /// Validates input file paths.
    /// </summary>
    private void ValidateInputs(string inputFilePath, string outputFilePath)
    {
        if (string.IsNullOrWhiteSpace(inputFilePath))
        {
            throw new ArgumentException("Input file path cannot be empty", nameof(inputFilePath));
        }

        if (string.IsNullOrWhiteSpace(outputFilePath))
        {
            throw new ArgumentException("Output file path cannot be empty", nameof(outputFilePath));
        }

        if (!_fileSystem.FileExists(inputFilePath))
        {
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");
        }
    }

    private async Task<List<string>> SortChunksAsync(
        string inputFilePath,
        string tempDirectory,
        long chunkSizeBytes,
        CancellationToken cancellationToken)
    {
        var sortedChunkFiles = new ConcurrentBag<string>();
        var chunkQueue = new BlockingCollection<(int ChunkId, List<LineEntry> Entries)>(boundedCapacity: Environment.ProcessorCount * 2);

        var readerTask = ReadAndQueueChunksAsync(inputFilePath, chunkQueue, chunkSizeBytes, cancellationToken);

        int processorCount = Environment.ProcessorCount;
        var workerTasks = Enumerable.Range(0, processorCount)
            .Select(_ => Task.Run(() =>
                ProcessChunkQueueAsync(chunkQueue, tempDirectory, sortedChunkFiles, cancellationToken), cancellationToken))
            .ToArray();

        await readerTask;
        chunkQueue.CompleteAdding();

        await Task.WhenAll(workerTasks);

        return [.. sortedChunkFiles.OrderBy(f => f)];
    }

    /// <summary>
    /// Continuously takes chunks from the queue, sorts them, and saves each to a temp file
    /// until the queue is marked as complete.
    /// </summary>
    private async Task ProcessChunkQueueAsync(
        BlockingCollection<(int ChunkId, List<LineEntry> Entries)> chunkQueue,
        string tempDirectory,
        ConcurrentBag<string> sortedChunkFiles,
        CancellationToken cancellationToken)
    {
        while (chunkQueue.TryTake(out (int ChunkId, List<LineEntry> Entries) chunk, Timeout.Infinite, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            chunk.Entries.Sort(_lineComparer);

            string tempFilePath = _fileSystem.Combine(tempDirectory, $"chunk_{chunk.ChunkId:D10}.tmp");
            await SaveChunkToFileAsync(tempFilePath, chunk.Entries, cancellationToken);

            sortedChunkFiles.Add(tempFilePath);
        }
    }

    private async Task ReadAndQueueChunksAsync(
        string inputFilePath,
        BlockingCollection<(int, List<LineEntry>)> chunkQueue,
        long chunkSizeBytes,
        CancellationToken cancellationToken)
    {
        using FileStream fileStream = _fileSystem.OpenRead(inputFilePath);
        using var reader = new StreamReader(fileStream, Encoding.UTF8, bufferSize: Constants.FileSorting.FileReaderBufferSize);
        var currentChunk = new List<LineEntry>();
        long currentChunkSize = 0;
        int chunkId = 0;
        string? line;

        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (LineParser.TryParse(line, out LineEntry? entry) && entry != null)
            {
                currentChunk.Add(entry);
                currentChunkSize += line.Length + Environment.NewLine.Length;

                if (currentChunkSize >= chunkSizeBytes)
                {
                    chunkQueue.Add((chunkId, currentChunk), cancellationToken);
                    chunkId++;
                    currentChunk = [];
                    currentChunkSize = 0;
                }
            }
        }

        if (currentChunk.Count > 0)
        {
            chunkQueue.Add((chunkId, currentChunk), cancellationToken);
        }
    }

    private async Task SaveChunkToFileAsync(
        string filePath,
        List<LineEntry> entries,
        CancellationToken cancellationToken)
    {
        using FileStream fileStream = _fileSystem.Create(filePath);
        using var writer = new StreamWriter(fileStream, Encoding.UTF8, bufferSize: Constants.FileSorting.FileWriterBufferSize);
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteEntry(writer, entry);
        }
        await writer.FlushAsync(cancellationToken);
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

    private async Task<long> MergeSortedChunksAsync(
        List<string> sortedChunkFiles,
        string outputFilePath,
        CancellationToken cancellationToken)
    {
        if (sortedChunkFiles.Count == 0)
        {
            _fileSystem.WriteAllText(outputFilePath, string.Empty);
            return 0;
        }

        if (sortedChunkFiles.Count == 1)
        {
            _fileSystem.Copy(sortedChunkFiles[0], outputFilePath, overwrite: true);
            return CountLinesInFile(sortedChunkFiles[0], cancellationToken);
        }

        string tempDirectory = _tempDirectoryManager.CreateTempDirectory();

        try
        {
            string finalFile = await MergeSortedChunksRecursiveAsync(sortedChunkFiles, tempDirectory, 0, cancellationToken);

            _fileSystem.Move(finalFile, outputFilePath, overwrite: true);

            return CountLinesInFile(outputFilePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while merging sorted chunks.");
            throw;
        }
        finally
        {
            _tempDirectoryManager.CleanupTempDirectory(tempDirectory);
        }
    }

    private async Task<string> MergeSortedChunksRecursiveAsync(
        List<string> chunkFiles,
        string tempDirectory,
        int recursionLevel,
        CancellationToken cancellationToken)
    {
        if (chunkFiles.Count <= Constants.FileSorting.MaxConcurrentReaders)
        {
            string mergedFile = _fileSystem.Combine(tempDirectory, $"merged_{Guid.NewGuid():N}.tmp");
            await _chunkMerger.MergeAsync(chunkFiles, mergedFile, cancellationToken);
            DeleteConsumedFiles(chunkFiles);
            return mergedFile;
        }

        var intermediateFiles = new List<string>();

        foreach (var batch in chunkFiles.Chunk(Constants.FileSorting.MaxConcurrentReaders))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string mergedBatch = _fileSystem.Combine(tempDirectory, $"batch_{Guid.NewGuid():N}.tmp");

            await _chunkMerger.MergeAsync([.. batch], mergedBatch, cancellationToken);
            DeleteConsumedFiles(batch);
            intermediateFiles.Add(mergedBatch);
        }

        return await MergeSortedChunksRecursiveAsync(intermediateFiles, tempDirectory, recursionLevel + 1, cancellationToken);
    }

    /// <summary>
    /// Deletes chunk/intermediate files that have already been merged into a later-stage file
    /// and are no longer needed, to avoid accumulating disk usage across recursive merge levels.
    /// Deletion failures are logged as warnings but do not fail the overall sort operation.
    /// </summary>
    private void DeleteConsumedFiles(IReadOnlyList<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            try
            {
                _fileSystem.Delete(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete consumed temp file: {FilePath}", filePath);
            }
        }
    }

    private long CountLinesInFile(string filePath, CancellationToken cancellationToken)
    {
        long count = 0;
        using var reader = new StreamReader(filePath);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            count++;
        }

        return count;
    }
}
