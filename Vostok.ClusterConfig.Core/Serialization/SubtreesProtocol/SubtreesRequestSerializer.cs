using System;
using System.Collections.Generic;
using System.Text;
using Vostok.Commons.Binary;

namespace Vostok.ClusterConfig.Core.Serialization.SubtreesProtocol
{
    internal static class SubtreesRequestSerializer
    {
        public static void Serialize(IBinaryWriter writer, List<(string prefix, DateTime? version, bool forceFullUpdate)> subtreesRequest)
        {
            writer.WriteCollection(subtreesRequest,
                (binaryWriter, subtree) =>
                {
                    binaryWriter.WriteWithLength(subtree.prefix.ToString());
                    binaryWriter.WriteNullable(subtree.version, (bw, time) => bw.Write(time.ToUniversalTime().Ticks));
                    binaryWriter.Write(subtree.forceFullUpdate);
                });
        }

        public static List<(string prefix, DateTime? version, bool forceFullUpdate)> Deserialize(IBinaryReader deserializer, Encoding encoding)
        {
            var count = deserializer.ReadInt32();
            var prefixes = new List<(string, DateTime?, bool)>();
            for (var i = 0; i < count; i++)
            {
                var prefix = deserializer.ReadString(encoding);
                var hasVersion = deserializer.ReadBool();
                var version = hasVersion 
                    ? new DateTime(deserializer.ReadInt64(), DateTimeKind.Utc) 
                    : (DateTime?)null;
                var forceFullUpdate = deserializer.ReadBool();
                
                prefixes.Add((prefix, version, forceFullUpdate));
            }

            return prefixes;
        }
    }
}