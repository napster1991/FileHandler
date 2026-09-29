using System.Diagnostics.CodeAnalysis;

namespace FileHandler.Services;

/// <summary>
/// Implementation of IFileSystem that wraps System.IO file operations.
/// </summary>
[ExcludeFromCodeCoverage]
public class FileManager : IFileManager
{
    public bool FileExists(string path) => File.Exists(path);

    public long GetFileLength(string path) => new FileInfo(path).Length;

    public FileStream OpenRead(string path) => File.OpenRead(path);

    public FileStream Create(string path) => File.Create(path);

    public void Copy(string sourceFileName, string destFileName, bool overwrite = false)
        => File.Copy(sourceFileName, destFileName, overwrite);

    public void Move(string sourceFileName, string destFileName, bool overwrite = false)
        => File.Move(sourceFileName, destFileName, overwrite);

    public void Delete(string path) => File.Delete(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void DeleteDirectory(string path, bool recursive = false)
        => Directory.Delete(path, recursive);

    public void WriteAllText(string path, string contents)
        => File.WriteAllText(path, contents);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public string GetTempPath() => Path.GetTempPath();

    public string GetDirectoryName(string path) => Path.GetDirectoryName(path);

    public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);

    public string Combine(params string[] paths) => Path.Combine(paths);
}
