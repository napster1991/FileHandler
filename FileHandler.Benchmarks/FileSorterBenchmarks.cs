using BenchmarkDotNet.Attributes;
using FileHandler.Features.SortFile.Services;
using FileHandler.Models;
using FileHandler.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FileHandler.Benchmarks;

/// <summary>
/// Benchmarks the end-to-end <see cref="FileSorter.SortFileAsync"/> pipeline (chunking, sorting,
/// and k-way merging) across different input sizes, to measure throughput and memory allocations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 3)]
public class FileSorterBenchmarks
{
    private string _tempDirectory = null!;
    private string _inputFilePath = null!;
    private string _outputFilePath = null!;
    private FileSorter _sorter = null!;

    /// <summary>
    /// Approximate input file size, in megabytes, to sort for this benchmark iteration.
    /// </summary>
    [Params(10, 50, 200, 500, 1000)]
    public long FileSizeMb;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"FileSorterBenchmarks_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);

        _inputFilePath = Path.Combine(_tempDirectory, "input.txt");
        GenerateInputFile(_inputFilePath, FileSizeMb);

        var fileSystem = new FileManager();
        var lineComparer = new LineEntryComparer();
        var chunkMerger = new KWayChunkMerger(fileSystem, lineComparer);
        var tempDirectoryManager = new TempDirectoryManager(NullLogger<TempDirectoryManager>.Instance);

        _sorter = new FileSorter(
            NullLogger<FileSorter>.Instance,
            tempDirectoryManager,
            fileSystem,
            lineComparer,
            chunkMerger);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _outputFilePath = Path.Combine(_tempDirectory, $"output_{Guid.NewGuid():N}.txt");
    }

    [Benchmark]
    public async Task<long> SortFileAsync()
    {
        return await _sorter.SortFileAsync(_inputFilePath, _outputFilePath);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private static void GenerateInputFile(string filePath, long fileSizeMb)
    {
        long targetBytes = fileSizeMb * Constants.FileSorting.BytesPerMegabyte;
        var texts = new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig", "Grape", "Honeydew" };
        var random = new Random(42);

        using var writer = new StreamWriter(filePath);
        long bytesWritten = 0;
        while (bytesWritten < targetBytes)
        {
            string text = texts[random.Next(texts.Length)];
            long number = random.NextInt64(1, long.MaxValue);
            string line = $"{number}. {text}";
            writer.WriteLine(line);
            bytesWritten += line.Length + Environment.NewLine.Length;
        }
    }
}
