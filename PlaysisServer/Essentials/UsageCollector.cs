using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace PlaysisServer.Essentials
{
    public static class UsageCollector
    {
        static readonly ConcurrentQueue<string> Queue = new();
        static string _anonSecret = "";
        static Thread? _writer;
        static int _written;

        public static bool Enabled { get; private set; }

        public static void Init(bool enabled, string anonSecret)
        {
            Enabled = enabled;
            if (!enabled) return;

            _anonSecret = string.IsNullOrEmpty(anonSecret) ? "" : anonSecret;
            if (_anonSecret.Length == 0)
            {
                Console.WriteLine("[Usage] 未配置 usageAnonSecret，采集关闭（避免可反推的匿名 ID）");
                Enabled = false;
                return;
            }

            Directory.CreateDirectory("./data/usage");

            _writer = new Thread(WriterLoop) { IsBackground = true };
            _writer.Start();
            Console.WriteLine("[Usage] 采集已开启 -> ./data/usage/");
        }

        public static string AnonUid(string uid)
        {
            if (_anonSecret.Length == 0) return "0";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_anonSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(uid.ToString()));
            var sb = new StringBuilder(16);
            for (var i = 0; i < 8; i++) sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }

        public static void Record(string uid, int roomId, string action, string modelId = "", int score = 0,
                                 int annotationCount = 0, int dwellSeconds = 0)
        {
            if (!Enabled) return;

            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var line = "{" +
                       $"\"ts\":{ts}," +
                       $"\"anonUid\":\"{AnonUid(uid)}\"," +
                       $"\"roomId\":{roomId}," +
                       $"\"action\":\"{action}\"," +
                       $"\"modelId\":{modelId}," +
                       $"\"score\":{score}," +
                       $"\"annotationCount\":{annotationCount}," +
                       $"\"dwellSeconds\":{dwellSeconds}" +
                       "}";

            Queue.Enqueue(line);
        }

        static void WriterLoop()
        {
            var buffer = new List<string>(256);

            while (true)
            {
                Thread.Sleep(5000);

                buffer.Clear();
                while (Queue.TryDequeue(out var line)) buffer.Add(line);
                if (buffer.Count == 0) continue;

                try
                {
                    var path = $"./data/usage/{DateTime.Now:yyyy-MM-dd}.jsonl";
                    File.AppendAllLines(path, buffer);
                    Interlocked.Add(ref _written, buffer.Count);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Usage] 写入失败: " + ex.Message);
                }
            }
        }
    }
}



