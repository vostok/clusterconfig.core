using System;
using System.Linq;
using System.Text;
using Vostok.ClusterConfig.Core.Patching;
using Vostok.ClusterConfig.Core.Utils;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal class NodeWriter
    {
        private readonly Encoding encoding;

        public NodeWriter(IBinaryWriter writer, Encoding encoding)
        {
            Writer = writer;
            
            this.encoding = encoding;
        }

        public IBinaryWriter Writer { get; }
        
        public BinaryWriterExtensions.ContentLengthCounter WriteHeader(NodeType type)
        {
            Writer.Write((byte)type);
            return Writer.AddContentLength();
        }
        
        public void WriteHeader(NodeType type, int length)
        {
            Writer.Write((byte) type);
            Writer.Write(length);
        }

        public void WriteKey(string key) => Writer.WriteWithLength(key ?? throw new InvalidOperationException("Key can't be null"));
        
        public void WriteNode(ISettingsNode node)
        {
            using var _ = WriteHeader(GetNodeType(node));
            
            switch (node)
            {
                case ObjectNode obj:
                    WriteNode(obj);
                    break;
                case ArrayNode array:
                    WriteNode(array);
                    break;
                case ValueNode value:
                    WriteNode(value);
                    break;
                case DeleteNode _:
                    break;
                default: throw new InvalidOperationException($"Unknown node type {node?.GetType().Name ?? "null"}");
            }
        }
        
        private void WriteNode(ObjectNode node)
        {
            Writer.Write(node.ChildrenCount);

            foreach (var child in node.Children.OrderBy(n => n.Name, Comparers.NodeNameComparer))
            {
                WriteKey(child.Name);
                WriteNode(child);
            }
        }

        private void WriteNode(ArrayNode node)
        {
            Writer.Write(node.ChildrenCount);

            foreach (var child in node.Children)
                WriteNode(child);
        }

        private void WriteNode(ValueNode node) =>
            Writer.WriteWithoutLength(node.Value ?? string.Empty, encoding);

        private static NodeType GetNodeType(ISettingsNode node) => node switch
        {
            ObjectNode _ => NodeType.Object,
            ArrayNode _ => NodeType.Array,
            ValueNode _ => NodeType.Value,
            DeleteNode _ => NodeType.Delete,
            _ => throw new ArgumentOutOfRangeException(nameof(node), node?.GetType().Name ?? "null")
        };
    }
}