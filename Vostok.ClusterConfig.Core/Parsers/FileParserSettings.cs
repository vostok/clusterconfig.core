using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.ClusterConfig.Core.Parsers.Content;

namespace Vostok.ClusterConfig.Core.Parsers
{
    internal class FileParserSettings
    {
        private Dictionary<string, int> maximumFileSizeZoneOverloads;
        private Dictionary<string, int> maximumFileSizeFileNameOverloads;

        public FileParserSettings()
        {
            MaximumFileSizeZoneOverloads = new Dictionary<string, int>
            {
                ["default"] = 200 * 1024
            };

            MaximumFileSizeFileNameOverloads = new Dictionary<string, int>
            {
                ["innBlackList"] = 500 * 1024
            };
        }
        
        public IFileContentParser DefaultParser { get; set; } = new KeyValueParser();

        public Dictionary<string, IFileContentParser> CustomParsers { get; set; } = new Dictionary<string, IFileContentParser>
        {
            [".example"] = NullParser.Instance,
            [".xml"] = WholeFileParser.Instance,
            [".xslt"] = WholeFileParser.Instance,
            [".json"] = WholeFileParser.Instance,
            [".cs"] = WholeFileParser.Instance,
            [".cer"] = Base64Parser.Instance,
            [".yml"] = WholeFileParser.Instance,
            [".yaml"] = WholeFileParser.Instance,
            [".toml"] = WholeFileParser.Instance,
            [".bin"] = WholeFileParser.Instance,
            [".sql"] = WholeFileParser.Instance,
            [".md"] = WholeFileParser.Instance
        };

        public int MaximumFileSize { get; set; } = 1024 * 1024;

        public Dictionary<string, int> MaximumFileSizeZoneOverloads
        {
            get => maximumFileSizeZoneOverloads;
            set => maximumFileSizeZoneOverloads = value.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<string, int> MaximumFileSizeFileNameOverloads
        {
            get => maximumFileSizeFileNameOverloads;
            set => maximumFileSizeFileNameOverloads = value.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
