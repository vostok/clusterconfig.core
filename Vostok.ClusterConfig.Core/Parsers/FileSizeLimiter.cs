using System.Collections.Generic;
using System.IO;

namespace Vostok.ClusterConfig.Core.Parsers
{
    public class FileSizeLimiter : IFileSizeLimiter
    {
        private readonly long maxFileSize;
        private readonly Dictionary<string, long> zoneToMaxFileSizeOverloads;
        private readonly Dictionary<string, long> fileNameToMaxFileSizeOverloads;

        public FileSizeLimiter(long maxFileSize, Dictionary<string, long> zoneToMaxFileSizeOverloads, Dictionary<string, long> fileNameToMaxFileSizeOverloads)
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