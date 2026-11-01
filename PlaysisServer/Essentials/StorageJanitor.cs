using System;
using System.Collections.Generic;
using System.IO;

namespace PlaysisServer.Essentials
{
    public static class StorageJanitor
    {
        public const string StorageDir = "./ModelStorage";

        public static (int scanned, int removed, int kept) Run(IEnumerable<string> referencedHashes, int minAgeDays)
        {
            if (!Directory.Exists(StorageDir)) return (0, 0, 0);

            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in referencedHashes)
                if (!string.IsNullOrWhiteSpace(h)) referenced.Add(h.Trim());

            var now = DateTime.UtcNow;
            var scanned = 0;
            var removed = 0;
            var kept = 0;

            foreach (var file in Directory.GetFiles(StorageDir))
            {
                scanned++;
                var name = Path.GetFileName(file);

                if (referenced.Contains(name))
                {
                    kept++;
                    continue;
                }

                var ageDays = (now - File.GetLastWriteTimeUtc(file)).TotalDays;
                if (ageDays < minAgeDays)
                {
                    kept++;
                    continue;
                }

                try
                {
                    File.Delete(file);
                    removed++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[StorageJanitor] delete failed: " + name + " " + ex.Message);
                    kept++;
                }
            }

            return (scanned, removed, kept);
        }
    }
}
