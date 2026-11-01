using System.Security.Cryptography;

namespace HuanyuAPI.Auth
{
    public static class AuthTokenStore
    {
        public const int DefaultDays = 30;

        static string Dir(string uid) => "./PlaysisUsers/UID/" + uid + "/";
        static string FileOf(string uid) => Dir(uid) + "token.txt";

        public static string Issue(string uid, int days)
        {
            var dir = Dir(uid);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var expires = now + (long)Math.Max(1, days) * 24 * 60 * 60 * 1000;

            File.WriteAllText(FileOf(uid), token + "\n" + now + "\n" + expires);
            return token;
        }

        public static bool TryValidate(string uid, string token, out long expiresAt)
        {
            expiresAt = 0;
            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(token)) return false;

            var path = FileOf(uid);
            if (!File.Exists(path)) return false;

            var lines = File.ReadAllLines(path);
            if (lines.Length < 3) return false;
            if (!string.Equals(lines[0], token, StringComparison.Ordinal)) return false;
            if (!long.TryParse(lines[2], out expiresAt)) return false;

            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < expiresAt;
        }

        public static long PeekExpiry(string uid)
        {
            var path = FileOf(uid);
            if (!File.Exists(path)) return 0;
            var lines = File.ReadAllLines(path);
            return lines.Length >= 3 && long.TryParse(lines[2], out var exp) ? exp : 0;
        }

        public static void Revoke(string uid)
        {
            var path = FileOf(uid);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    public sealed class TokenLoginRequest
    {
        public string Uid { get; set; } = "";
        public string Token { get; set; } = "";
    }

    public sealed class LogoutRequest
    {
        public string Uid { get; set; } = "";
        public string Secret { get; set; } = "";
    }

    public static class SchoolAuthConfig2
    {
        public static int TokenDays
        {
            get
            {
                var raw = Program.Config["schoolAuthTokenDays"] as string;
                return int.TryParse(raw, out var d) && d > 0 ? d : AuthTokenStore.DefaultDays;
            }
        }
    }
}
