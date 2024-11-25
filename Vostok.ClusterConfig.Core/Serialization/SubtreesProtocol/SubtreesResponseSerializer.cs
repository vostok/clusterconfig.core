using System;
using System.Collections.Generic;
using System.Text;
using Vostok.Commons.Binary;

namespace Vostok.ClusterConfig.Core.Serialization.SubtreesProtocol
{
    //[SubtreesCount (int32)]
    //    [Prefix + Length (int32 + string_utf8)]
    //    [WasModified (byte)]
    //    if (WasModified)
    //        [HasSubtree (byte)]
    //        if (HasSubtree) 
    //            [IsPatch (byte)]
    //            [IsCompressed (byte)]
    //            [SerializedTree + Length (int32 + byte_array)]
    internal static class SubtreesResponseSerializer
    {
        public static void Serialize(IBinaryWriter writer, Dictionary<string, Subtree> subtrees)
        {
            writer.Write(subtrees.Count);
            foreach (var pair in subtrees)
            {
                var prefix = pair.Key;
                var subtree = pair.Value;
                
                writer.WriteWithLength(prefix);
                writer.Write(subtree.WasModified);
                if (subtree.WasModified)
                {
                    writer.Write(subtree.SubtreeExists);
                    if (subtree.SubtreeExists)
                    {
                        writer.Write(subtree.IsPatch);
                        writer.Write(subtree.IsCompressed);
                        writer.WriteWithLength(subtree.Content.Array!, subtree.Content.Offset, subtree.Content.Count);
                    }
                }
            }
        }

        public static Dictionary<string, Subtree> Deserialize(BinaryBufferReader reader, Encoding encoding, bool autoDecompress = true)
        {
            var subtreesCount = reader.ReadInt32();
            var subtrees = new Dictionary<string, Subtree>(subtreesCount);
            for (var i = 0; i < subtreesCount; i++)
            {
                var path = reader.ReadString(encoding);

                var wasModified = false;
                var subtreeExists = false;
                var isCompressed = false;
                var isPatch = false;
                ArraySegment<byte> segment = default;

                wasModified = reader.ReadBool();
                if (wasModified)
                {
                    subtreeExists = reader.ReadBool();
                    if (subtreeExists)
                    {
                        isPatch = reader.ReadBool();
                        isCompressed = reader.ReadBool();
                        var arrayLength = reader.ReadInt32();
                        segment = new ArraySegment<byte>(reader.Buffer, (int)reader.Position, arrayLength);

                        reader.Position += arrayLength;
                    }
                }

                var subtree = new Subtree(wasModified, subtreeExists, isPatch, isCompressed, segment);
                if (autoDecompress)
                    subtree.DecompressIfNeeded();

                subtrees.Add(path, subtree);
            }
            
            return subtrees;
        }
    }
}