namespace NexaDesk;

public static class AppPaths
{
    public static string RootDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NexaDesk");

    public static string DataDirectory { get; } = Path.Combine(RootDirectory, "data");
    public static string DatabasePath { get; } = Path.Combine(DataDirectory, "nexadesk.db");
    public static string LogDirectory { get; } = Path.Combine(RootDirectory, "logs");
    public static string LogPath { get; } = Path.Combine(LogDirectory, "nexadesk.log");
    public static string StartupProbePath { get; } = Path.Combine(RootDirectory, "startup-probe.txt");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}
