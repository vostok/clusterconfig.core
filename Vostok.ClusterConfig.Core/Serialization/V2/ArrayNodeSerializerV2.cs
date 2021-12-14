using System.Collections.Generic;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal class ArrayNodeSerializerV2 : ReplaceableNodeSerializerV2<ArrayNode>
    {
        private readonly BaseSettingsNodeSerializerV2<ISettingsNode> any;

        public ArrayNodeSerializerV2(BaseSettingsNodeSerializerV2<ISettingsNode> any) => this.any = any;

        public override void Serialize(ArrayNode node, IBinaryWriter writer)
        {
            using (Node(writer, NodeType.Array))
            {
                writer.Write(node.ChildrenCount);

                foreach (var child in node.Children)
                    any.Serialize(child, writer);
            }
        }

        public override ArrayNode Deserialize(IBinaryReader reader, string name)
        {
            EnsureType(reader, NodeType.Array);

            var count = reader.ReadInt32();

            var children = new List<ISettingsNode>(count);

            for (var i = 0; i < count; i++)
                children.Add(any.Deserialize(reader, i.ToString()));

            return new ArrayNode(name, children);
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