namespace AllyConsole.Core.Tests;

internal sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Path = Directory.CreateTempSubdirectory("ally-console-tests-").FullName;
    }

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // The test assertion already ran. Leaving a temp directory is better than hiding a failure.
        }
    }
}
