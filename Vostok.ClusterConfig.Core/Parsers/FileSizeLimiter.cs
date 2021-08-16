using System.Collections.Generic;
using System.IO;

namespace Vostok.ClusterConfig.Core.Parsers
{
    internal class FileSizeLimiter : IFileSizeLimiter
    {
        private readonly long maxFileSize;
        private readonly Dictionary<string, int> zoneToMaxFileSizeOverloads;
        private readonly Dictionary<string, int> fileNameToMaxFileSizeOverloads;

        public FileSizeLimiter(long maxFileSize, Dictionary<string, int> zoneToMaxFileSizeOverloads, Dictionary<string, int> fileNameToMaxFileSizeOverloads)
        {
            this.maxFileSize = maxFileSize;
            this.zoneToMaxFileSizeOverloads = zoneToMaxFileSizeOverloads;
            this.fileNameToMaxFileSizeOverloads = fileNameToMaxFileSizeOverloads;
        }
        
        public bool IsSizeAcceptable(FileInfo file, string zone) => GetLimit(file, zone) >= file.Length;

        private long GetLimit(FileInfo file, string zone)
        {
            if (fileNameToMaxFileSizeOverloads.TryGetValue(file.Name, out var limit))
                return limit;

            if (zoneToMaxFileSizeOverloads.TryGetValue(zone, out limit))
                return limit;

            return maxFileSize;
        }
    }
}