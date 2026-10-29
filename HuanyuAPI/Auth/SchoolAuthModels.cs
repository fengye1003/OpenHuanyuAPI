using System.Collections.Concurrent;

namespace HuanyuAPI.Auth
{
    public sealed class SchoolLoginRequest
    {
        public string School { get; set; } = "";
        public string ShortId { get; set; } = "";
        public string FullId { get; set; } = "";
        public string Nonce { get; set; } = "";
        public long IssuedAt { get; set; }
        public Dictionary<string, string> Cookies { get; set; } = new();
    }

    public static class SchoolAuthConfig
    {
        public const string DefaultYear = "2025";
        public const string NjtechKey = "njtech";

        public static string Year =>
            (string?)Program.Config["schoolAuthYear"] ?? DefaultYear;

        public static string FullIdOf(string shortId) => Year + shortId;
    }

    public static class NonceCache
    {
        private static readonly ConcurrentDictionary<string, long> Seen = new();

        public static bool TryUse(string nonce, long nowMs, long windowMs = 300_000)
        {
            foreach (var kv in Seen)
                if (nowMs - kv.Value > windowMs) Seen.TryRemove(kv.Key, out _);

            if (string.IsNullOrWhiteSpace(nonce)) return false;
            return Seen.TryAdd(nonce, nowMs);
        }
    }
}
