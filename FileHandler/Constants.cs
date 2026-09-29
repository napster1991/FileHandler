namespace FileHandler;

public static class Constants
{
    public static class FileSorting
    {
        // Memory-optimized sorter configuration
        public const long MemoryOptimizedMinChunkSizeBytes = 100L * 1024 * 1024; // 100MB minimum
        public const long MemoryOptimizedDefaultMaxChunkSizeBytes = 500L * 1024 * 1024; // 500MB default
        public const int MaxConcurrentReaders = 64; // Safe limit for file descriptors across all OS

        // Buffer sizes for file I/O
        public const int FileReaderBufferSize = 65536; // 64KB
        public const int FileWriterBufferSize = 65536; // 64KB

        // Throughput-optimized sorter configuration
        public const long ThroughputOptimizedDefaultMaxChunkSizeBytes = 3L * 1024 * 1024 * 1024; // 3GB default

        // Test file generator configuration
        public const int TestFileGeneratorTextPoolSize = 20;
        public const double TestFileGeneratorDuplicateTextRatio = 0.7;
        public const int TestFileGeneratorMinTextLength = 5;
        public const int TestFileGeneratorMaxTextLength = 15;
        public const long BytesPerMegabyte = 1024 * 1024;
    }
}
