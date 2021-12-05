using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Vostok.ClusterConfig.Core.Serialization.V2;
using Vostok.Commons.Binary;
using Vostok.Commons.Collections;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization
{
    // Every node format:
    //  + node type (byte)
    //  + node content length (int)
    //  + node content (detailed below)
    
    // ObjectNode content format:
    //  + pairs count (byte)
    //  + pairs, every pair:
    //    + Key length in bytes (int)
    //    + Key (UTF-8 string)
    //    + Value: any node

    // ArrayNode content format:
    //  + items count (byte)
    //  + nodes

    // ValueNode content format:
    //  + length in bytes (int)
    //  + UTD-8 string
    
    //  DeleteNode doesn't have a content

    internal class TreeSerializerV2 : ITreeSerializerV2
    {
        private static readonly UnboundedObjectPool<BinaryBufferWriter> WriterPool
            = new UnboundedObjectPool<BinaryBufferWriter>(() => new BinaryBufferWriter(4096));

        private readonly AnyNodeSerializerV2 anyNodeSerializer;

        public TreeSerializerV2() => anyNodeSerializer = new AnyNodeSerializerV2();

        public void Serialize([CanBeNull] ISettingsNode tree, IBinaryWriter writer)
        {
            if (tree != null)
                anyNodeSerializer.Serialize(tree, writer);
        }

        public ISettingsNode Deserialize(byte[] tree)
        {
            return tree.Any() ? anyNodeSerializer.Deserialize(new BinaryBufferReader(tree, 0), null) : null;
        }

        public ISettingsNode Deserialize(byte[] tree, IEnumerable<string> path)
        {
            using (var pathEnumerator = path.GetEnumerator())
                return tree.Any() ? anyNodeSerializer.Deserialize(new BinaryBufferReader(tree, 0), pathEnumerator, null) : null;
        }

        public byte[] ApplyPatch(byte[] settings, byte[] patch)
        {
            if (!patch.Any())
                return settings;

            if (!settings.Any())
                return patch;
            
            using (WriterPool.Acquire(out var writer))
            {
                writer.Reset();

                anyNodeSerializer.ApplyPatch(new BinaryBufferReader(settings, 0), new BinaryBufferReader(patch, 0), writer);

                var result = new byte[writer.Length];
                
                Array.Copy(writer.Buffer, result, writer.Length);

                return result;
            }
        }
    }
}