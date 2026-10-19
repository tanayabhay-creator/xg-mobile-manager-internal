using System.Text.Json;
using AllyConsole.Core.Logging;

namespace AllyConsole.Core.Tests;

public class RotatingLogTests
{
    [Fact]
    public void Writes_structured_lines_and_rotates_oldest_away()
    {
        using var folder = new TempFolder();
        var log = new RotatingLog(folder.Path, maxBytes: 240, maxFiles: 3);
        log.Write("info", "test.first", "FIRST_TOKEN_XYZ");

        var first = File.ReadAllText(log.ActivePath);
        using (var document = JsonDocument.Parse(first.Trim()))
        {
            Assert.Equal("info", document.RootElement.GetProperty("level").GetString());
            Assert.Equal("test.first", document.RootElement.GetProperty("event").GetString());
            Assert.Equal("FIRST_TOKEN_XYZ", document.RootElement.GetProperty("message").GetString());
            Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("ts").GetString()));
        }

        for (var index = 0; index < 30; index++)
            log.Write("info", "test.later", "LATER_" + index.ToString("00"));

        var files = log.ExistingFiles();
        Assert.NotEmpty(files);
        Assert.True(files.Count <= 3);
        Assert.All(files, file => Assert.StartsWith(folder.Path, file, StringComparison.Ordinal));

        var combined = string.Join('\n', files.Select(File.ReadAllText));
        Assert.DoesNotContain("FIRST_TOKEN_XYZ", combined, StringComparison.Ordinal);
        Assert.Contains("LATER_29", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void Small_writes_stay_in_the_active_file()
    {
        using var folder = new TempFolder();
        var log = new RotatingLog(folder.Path, maxBytes: 100_000, maxFiles: 3);
        log.Write("info", "app.start", "started");

        Assert.Single(log.ExistingFiles());
        Assert.Contains("started", File.ReadAllText(log.ActivePath), StringComparison.Ordinal);
    }
}
