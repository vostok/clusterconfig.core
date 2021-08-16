using System.Collections.Generic;
using System.IO;
using Vostok.ClusterConfig.Core.Parsers.Content;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Parsers
{
    internal class FileParser : IFileParser
    {
        private readonly FileParserSettings settings;
        private readonly IFileSizeLimiter limiter;

        public FileParser(FileParserSettings settings)
        {
            this.settings = settings;
            limiter = new FileSizeLimiter(
                settings.MaximumFileSize,
                settings.MaximumFileSizeZoneOverloads ?? new Dictionary<string, int>(),
                settings.MaximumFileSizeFileNameOverloads ?? new Dictionary<string, int>());
        }

        public ObjectNode Parse(FileInfo file, string zone)
        {
            try
            {
                if (!limiter.IsSizeAcceptable(file, zone))
                    return null;

                var parser = settings.CustomParsers.TryGetValue(file.Extension.ToLower(), out var customParser)
                    ? customParser
                    : settings.DefaultParser;

                using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    return parser.Parse(file.Name.ToLower(), new FileContent(stream, (int) file.Length));
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}
