// Minimal shim so AppLanguage.cs compiles in the test project
// without a MAUI dependency. Uses an in-memory dictionary.

namespace Microsoft.Maui.Storage;

internal static class Preferences
{
    public static IPreferences Default { get; } = new InMemoryPreferences();

    private sealed class InMemoryPreferences : IPreferences
    {
        private readonly Dictionary<string, string> _store = new();

        public T Get<T>(string key, T defaultValue, string? sharedName = null)
        {
            if (_store.TryGetValue(key, out var raw) && raw is T typed)
                return typed;
            return defaultValue;
        }

        public void Set<T>(string key, T value, string? sharedName = null)
        {
            _store[key] = value?.ToString() ?? "";
        }

        public bool ContainsKey(string key, string? sharedName = null)
            => _store.ContainsKey(key);

        public void Remove(string key, string? sharedName = null)
            => _store.Remove(key);

        public void Clear(string? sharedName = null)
            => _store.Clear();
    }
}

internal interface IPreferences
{
    T Get<T>(string key, T defaultValue, string? sharedName = null);
    void Set<T>(string key, T value, string? sharedName = null);
    bool ContainsKey(string key, string? sharedName = null);
    void Remove(string key, string? sharedName = null);
    void Clear(string? sharedName = null);
}
