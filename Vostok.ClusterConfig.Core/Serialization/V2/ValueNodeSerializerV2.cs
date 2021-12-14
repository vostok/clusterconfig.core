using System;
using System.Collections.Generic;
using System.Text;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal class ValueNodeSerializerV2 : ReplaceableNodeSerializerV2<ValueNode>
    {
        private static readonly Encoding Encoding = Encoding.UTF8;
        
        public override void Serialize(ValueNode node, IBinaryWriter writer)
        {
            using (Node(writer, NodeType.Value))
                writer.WriteWithoutLength(Encoding.GetBytes(node.Value ?? String.Empty));
        }

        public override ValueNode Deserialize(IBinaryReader reader, string name)
        {
            EnsureType(reader, NodeType.Value, out var length);

            return new ValueNode(name, Encoding.GetString(reader.ReadByteArray(length)));
        }

        public override ISettingsNode Deserialize(IBinaryReader reader, IEnumerator<string> path, string name)
        {
            return path.MoveNext() ? null : Deserialize(reader, name);
        }

        public override void ApplyPatch(BinaryBufferReader settings, BinaryBufferReader patch, IBinaryWriter result)
        {
            SkipNode(settings);
            CopyNode(patch, result);
        }
    }
}