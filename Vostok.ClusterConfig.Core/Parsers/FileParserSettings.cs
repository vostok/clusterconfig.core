using System.Collections.Generic;
using Vostok.ClusterConfig.Core.Parsers.Content;

namespace Vostok.ClusterConfig.Core.Parsers
{
    internal class FileParserSettings
    {
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

        public Dictionary<string, long> MaximumFileSizeZoneOverloads { get; set; } = new Dictionary<string, long>
        {
            ["default"] = 200 * 1024
        };

        public Dictionary<string, long> MaximumFileSizeFileNameOverloads { get; set; } = new Dictionary<string, long>
        {
            ["innBlackList"] = 500 * 1024,
            ["__file_size_test_1K__"] = 1 * 1024,
            ["__file_size_test_2K__"] = 2 * 1024
        };
    }
}
