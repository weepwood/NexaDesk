using System.Security.Cryptography;
using System.Text;
using NexaDesk.Models;

namespace NexaDesk;

public sealed class ApplicationIndexService(DatabaseService database)
{
    public async Task RefreshIfStaleAsync()
    {
        string? lastScanText = await database.GetSettingAsync("last_app_scan_utc");
        if (DateTimeOffset.TryParse(lastScanText, out DateTimeOffset lastScan) &&
            DateTimeOffset.UtcNow - lastScan < TimeSpan.FromHours(24))
        {
            return;
        }

        await RefreshAsync();
    }

    public async Task<int> RefreshAsync()
    {
        string[] roots =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
        ];

        Dictionary<string, ActionDefinition> actions =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (string root in roots.Where(Directory.Exists))
        {
            try
            {
                foreach (string path in EnumerateApplicationLinks(root))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    string id = "app." + Convert.ToHexString(
                        SHA256.HashData(Encoding.UTF8.GetBytes(path)))
                        .ToLowerInvariant()[..24];

                    actions[id] = new ActionDefinition
                    {
                        Id = id,
                        Name = name,
                        Description = path,
                        Kind = ActionKind.LaunchFile,
                        Target = path,
                        Category = "应用",
                        IconGlyph = "\uE8A5"
                    };
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip protected Start menu folders and keep the usable index.
            }
            catch (IOException)
            {
                // A shortcut may disappear while the Start menu is being scanned.
            }
        }

        await database.UpsertIndexedActionsAsync(actions.Values);
        await database.SetSettingAsync(
            "last_app_scan_utc",
            DateTimeOffset.UtcNow.ToString("O"));

        return actions.Count;
    }

    private static IEnumerable<string> EnumerateApplicationLinks(string root)
    {
        return Directory.EnumerateFiles(
                root,
                "*.*",
                new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.ReparsePoint
                })
            .Where(static path =>
                path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".url", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".appref-ms", StringComparison.OrdinalIgnoreCase));
    }
}
