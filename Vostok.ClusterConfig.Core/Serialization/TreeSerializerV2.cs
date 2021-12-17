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
    //  + children count (int)
    //  + children, every child:
    //    + Key: UTF-8 string with length
    //    + Value: INode
    // (Children are ordered by key (OrdinalIgnoreCase))

    // ArrayNode content format:
    //  + children count (int)
    //  + children: INode[]

    // ValueNode content format:
    //  + UTF-8 string without length
    
    //  DeleteNode doesn't have a content

    internal class TreeSerializerV2 : ITreeSerializerV2
    {
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

        public void ApplyPatch(byte[] settings, byte[] patch, IBinaryWriter result)
        {
            if (!patch.Any())
            {
                result.WriteWithoutLength(settings);
            }
            else if (!settings.Any())
            {
                result.WriteWithoutLength(patch);
            }
            else
            {
                anyNodeSerializer.ApplyPatch(new BinaryBufferReader(settings, 0), new BinaryBufferReader(patch, 0), result);
            }
        }
    }
}