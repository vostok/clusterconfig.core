using System;
using System.Collections.Generic;
using Vostok.ClusterConfig.Core.Patching;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal class DeleteNodeSerializerV2 : ReplaceableNodeSerializerV2<DeleteNode>
    {
        public override void Serialize(DeleteNode node, IBinaryWriter writer)
        {
            using (Node(writer, NodeType.Delete)) ;
        }

        public override DeleteNode Deserialize(IBinaryReader reader, string name)
        {
            EnsureType(reader, NodeType.Delete);
            
            return new DeleteNode(name);
        }

        public override ISettingsNode Deserialize(IBinaryReader reader, IEnumerator<string> path, string name)
        {
            return path.MoveNext() ? null : Deserialize(reader, name);
        }

        public override void ApplyPatch(BinaryBufferReader settings, BinaryBufferReader patch, IBinaryWriter result)
        {
            throw new NotSupportedException("Can't merge delete node with delete node");
        }
    }
}