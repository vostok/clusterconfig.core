using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.ClusterConfig.Core.Patching;
using Vostok.ClusterConfig.Core.Serialization.V2;
using Vostok.Commons.Binary;
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

    internal class TreeSerializerV2 : ITreeSerializer, IBinaryPatcher
    {
        private readonly AnyNodeSerializerV2 anyNodeSerializer;

        public TreeSerializerV2() => anyNodeSerializer = new AnyNodeSerializerV2();

        public void Serialize([CanBeNull] ISettingsNode tree, IBinaryWriter writer)
        {
            if (tree != null)
                anyNodeSerializer.Serialize(tree, writer);
        }

        public ISettingsNode Deserialize(BinaryBufferReader tree)
        {
            return tree.BytesRemaining > 0 ? anyNodeSerializer.Deserialize(tree, null) : null;
        }

        public ISettingsNode Deserialize(BinaryBufferReader tree, IEnumerable<string> path)
        {
            using (var pathEnumerator = path.GetEnumerator())
                return tree.BytesRemaining > 0 ? anyNodeSerializer.Deserialize(tree, pathEnumerator, null) : null;
        }

        public void ApplyPatch(BinaryBufferReader settings, BinaryBufferReader patch, IBinaryWriter result)
        {
            if (patch.BytesRemaining == 0)
            {
                result.WriteWithoutLength(settings.Buffer, (int) settings.Position, (int) settings.BytesRemaining);
            }
            else if (settings.BytesRemaining == 0)
            {
                result.WriteWithoutLength(patch.Buffer, (int) patch.Position, (int) patch.BytesRemaining);
            }
            else
            {
                anyNodeSerializer.ApplyPatch(settings, patch, result);
            }
        }
    }
}