using System.Text;
using System.Text.Json;

namespace AllyConsole.Core.Logging;

public sealed class RotatingLog
{
    private readonly object _gate = new();
    private readonly JsonSerializerOptions _json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RotatingLog(string directory, int maxBytes, int maxFiles, string fileName = "ally-console.log")
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Log directory is required.", nameof(directory));
        if (maxBytes < 1)
            throw new ArgumentOutOfRangeException(nameof(maxBytes));
        if (maxFiles < 1)
            throw new ArgumentOutOfRangeException(nameof(maxFiles));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Log file name is required.", nameof(fileName));

        DirectoryPath = directory;
        MaxBytes = maxBytes;
        MaxFiles = maxFiles;
        FileName = fileName;
    }

    public string DirectoryPath { get; }

    public int MaxBytes { get; }

    public int MaxFiles { get; }

    public string FileName { get; }

    public string ActivePath => Path.Combine(DirectoryPath, FileName);

    public void Write(string level, string eventName, string message)
    {
        var line = JsonSerializer.Serialize(new LogLine(DateTimeOffset.UtcNow.ToString("O"), level, eventName, message), _json)
            + "\n";
        var incoming = Encoding.UTF8.GetByteCount(line);
        lock (_gate)
        {
            Directory.CreateDirectory(DirectoryPath);
            if (File.Exists(ActivePath))
            {
                var length = new FileInfo(ActivePath).Length;
                if (length > 0 && length + incoming > MaxBytes)
                    Rotate();
            }

            File.AppendAllText(ActivePath, line, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    public IReadOnlyList<string> ExistingFiles()
    {
        if (!Directory.Exists(DirectoryPath))
            return Array.Empty<string>();
        var stem = Path.GetFileNameWithoutExtension(FileName);
        var extension = Path.GetExtension(FileName);
        return Directory.GetFiles(DirectoryPath)
            .Where(path =>
            {
                var name = Path.GetFileName(path);
                return name.Equals(FileName, StringComparison.OrdinalIgnoreCase)
                    || (name.StartsWith(stem + ".", StringComparison.OrdinalIgnoreCase)
                        && name.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
            })
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void Rotate()
    {
        if (MaxFiles == 1)
        {
            File.Delete(ActivePath);
            return;
        }

        var oldest = Archive(MaxFiles - 1);
        if (File.Exists(oldest))
            File.Delete(oldest);

        for (var index = MaxFiles - 2; index >= 1; index--)
        {
            var source = Archive(index);
            if (File.Exists(source))
                File.Move(source, Archive(index + 1), overwrite: true);
        }

        if (File.Exists(ActivePath))
            File.Move(ActivePath, Archive(1), overwrite: true);
    }

    private string Archive(int index)
    {
        var stem = Path.GetFileNameWithoutExtension(FileName);
        var extension = Path.GetExtension(FileName);
        return Path.Combine(DirectoryPath, stem + "." + index + extension);
    }

    private sealed record LogLine(string Ts, string Level, string Event, string Message);
}
