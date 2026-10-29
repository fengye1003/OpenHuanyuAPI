using System.Text.Json;
using System.Text.RegularExpressions;

namespace HuanyuAPI.Auth
{
    public interface ISchoolVerifier
    {
        string Key { get; }
        Task<string?> VerifyIdentityAsync(Dictionary<string, string> cookies, CancellationToken ct);
    }

    public sealed class NjtechVerifier : ISchoolVerifier
    {
        private const string Ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36";

        private readonly string _baseUrl;
        private readonly HttpClient _http;

        public string Key => "njtech";

        public NjtechVerifier(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
            _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        }

        public async Task<string?> VerifyIdentityAsync(Dictionary<string, string> cookies, CancellationToken ct)
        {
            if (cookies.Count == 0) return null;
            var cookieHeader = string.Join("; ", cookies.Select(kv => $"{kv.Key}={kv.Value}"));

            var xh = await TryKbcxAsync(cookieHeader, ct);
            if (!string.IsNullOrEmpty(xh)) return xh;

            return await TryIndexPageAsync(cookieHeader, ct);
        }

        private async Task<string?> TryKbcxAsync(string cookieHeader, CancellationToken ct)
        {
            var (xnm, xqm) = CurrentTerm();
            var req = new HttpRequestMessage(HttpMethod.Post,
                $"{_baseUrl}/kbcx/xskbcx_cxXsgrkb.html?gnmkdm=N2151");
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["xnm"] = xnm,
                ["xqm"] = xqm
            });
            AddHeaders(req, cookieHeader);

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return null;

            var body = await resp.Content.ReadAsStringAsync(ct);
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
                if (!doc.RootElement.TryGetProperty("xsxx", out var xsxx)) return null;
                if (xsxx.ValueKind != JsonValueKind.Object) return null;
                if (!xsxx.TryGetProperty("XH", out var xh)) return null;
                return xh.GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SchoolLogin] kbcx 解析失败({ex.GetType().Name})，前 200 字：" +
                                  body.Substring(0, Math.Min(200, body.Length)));
                return null;
            }
        }

        private async Task<string?> TryIndexPageAsync(string cookieHeader, CancellationToken ct)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/xtgl/index_initMenu.html");
            AddHeaders(req, cookieHeader);
            req.Headers.TryAddWithoutValidation("Referer", $"{_baseUrl}/xtgl/index_initMenu.html");

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return null;

            var body = await resp.Content.ReadAsStringAsync(ct);
            var m = Regex.Match(body, @"(?<!\d)(20\d{10})(?!\d)");
            if (m.Success) return m.Groups[1].Value;

            Console.WriteLine("[SchoolLogin] index 页未找到 12 位学号，前 200 字：" +
                              body.Substring(0, Math.Min(200, body.Length)));
            return null;
        }

        private static void AddHeaders(HttpRequestMessage req, string cookieHeader)
        {
            req.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
            req.Headers.TryAddWithoutValidation("User-Agent", Ua);
            req.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
            req.Headers.TryAddWithoutValidation("Accept", "application/json, text/javascript, */*; q=0.01");
        }

        private static (string xnm, string xqm) CurrentTerm()
        {
            var now = DateTime.Now;
            var month = now.Month;
            var xqm = "3";
            if (month >= 2 && month <= 6) xqm = "12";
            else if (month >= 7 && month <= 8) xqm = "16";
            var xnm = month >= 8 ? now.Year : now.Year - 1;
            return (xnm.ToString(), xqm);
        }
    }
}
