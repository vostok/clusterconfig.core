using System;
using System.Collections.Generic;
using System.Text;
using Vostok.Commons.Binary;

namespace Vostok.ClusterConfig.Core.Serialization.SubtreesProtocol
{
    internal static class SubtreesRequestSerializer
    {
        public static void Serialize(IBinaryWriter writer, List<SubtreeRequest> subtreesRequest)
        {
            writer.WriteCollection(subtreesRequest,
                (binaryWriter, subtree) =>
                {
                    binaryWriter.WriteWithLength(subtree.Prefix);
                    binaryWriter.WriteNullable(subtree.Version, (bw, time) => bw.Write(time.ToUniversalTime().Ticks));
                    binaryWriter.Write(subtree.ForceFullUpdate);
                });
        }

        public static List<SubtreeRequest> Deserialize(IBinaryReader deserializer, Encoding encoding)
        {
            var count = deserializer.ReadInt32();
            var prefixes = new List<SubtreeRequest>();
            for (var i = 0; i < count; i++)
            {
                var prefix = deserializer.ReadString(encoding);
                var hasVersion = deserializer.ReadBool();
                var version = hasVersion 
                    ? new DateTime(deserializer.ReadInt64(), DateTimeKind.Utc) 
                    : (DateTime?)null;
                var forceFullUpdate = deserializer.ReadBool();
                
                prefixes.Add(new SubtreeRequest(prefix, version, forceFullUpdate));
            }

            return prefixes;
        }
    }
}