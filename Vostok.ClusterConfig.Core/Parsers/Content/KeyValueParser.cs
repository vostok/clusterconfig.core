using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using Vostok.Commons.Collections;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Parsers.Content
{
    internal class KeyValueParser : IFileContentParser
    {
        private const string CommentToken = "#";
        private static readonly string[] Separators = {":=", "="};
        private static readonly char[] ByteOrderMarks = { '\uFEFF' };
        
        private readonly RecyclingBoundedCache<string,string> interningCache;

        public KeyValueParser()
            : this(null)
        {
        }

        public KeyValueParser([CanBeNull] RecyclingBoundedCache<string, string> interningCache)
        {
            this.interningCache = interningCache;
        }

        public ObjectNode Parse(string name, IFileContent content)
        {
            var index = new Dictionary<string, List<string>>();
            
            var reader = new StreamReader(content.AsStream, Encoding.UTF8);

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine()?.Trim(ByteOrderMarks)?.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith(CommentToken))
                    continue;

                ParseLine(line, out var key, out var value);

                if (index.TryGetValue(key, out var values))
                {
                    values.Add(value);
                }
                else index[key] = new List<string> { value };
            }

            if (index.Count == 0)
                index.Add(string.Empty, new List<string> { string.Empty });

            return new ObjectNode(Intern(name), index.Select(pair => ConvertToNode(pair.Key, pair.Value)));
        }

        private static void ParseLine(string line, out string key, out string value)
        {
            foreach (var separator in Separators)
            {
                if (TryParseLine(line, separator, out key, out value))
                    return;
            }

            key = string.Empty;
            value = line;
        }

        private static bool TryParseLine(string line, string separator, out string key, out string value)
        {
            var separatorIndex = line.IndexOf(separator, StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                key = value = null;
                return false;
            }

            key = line.Substring(0, separatorIndex).Trim().ToLower();
            value = line.Substring(separatorIndex + separator.Length).Trim();
            return true;
        }

        private ISettingsNode ConvertToNode(string key, List<string> values)
        {
            if (values.Count == 1)
                return new ValueNode(Intern(key), Intern(values.Single()));

            var children = new ValueNode[values.Count];
            for (var i = 0; i < values.Count; i++) 
                children[i] = new ValueNode(Intern(i.ToString()), Intern(values[i]));

            return new ArrayNode(Intern(key), children);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string Intern(string value)
        {
            return interningCache == null 
                ? value 
                : interningCache.Obtain(value, x => x);
        }
    }
}
