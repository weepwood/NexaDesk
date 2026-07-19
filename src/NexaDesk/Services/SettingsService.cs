using System.Collections.Concurrent;

namespace NexaDesk;

public sealed class SettingsService(DatabaseService database)
{
    private static readonly string[] CachedKeys = ["theme", "last_app_scan_utc"];
    private readonly ConcurrentDictionary<string, string> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task LoadCacheAsync()
    {
        foreach (string key in CachedKeys)
        {
            string? value = await database.GetSettingAsync(key);
            if (value is not null)
            {
                _cache[key] = value;
            }
        }
    }

    public string GetCached(string key, string fallback) =>
        _cache.TryGetValue(key, out string? value) ? value : fallback;

    public async Task SetAsync(string key, string value)
    {
        _cache[key] = value;
        await database.SetSettingAsync(key, value);
    }
}
