using System;
using System.Collections.Generic;
using System.Text;
using Vostok.ClusterConfig.Core.Patching;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal class NodeReader
    {
        private readonly Encoding encoding;
        
        public NodeReader(BinaryBufferReader reader, Encoding encoding)
        {
            this.encoding = encoding;
            Reader = reader;
        }
        
        public BinaryBufferReader Reader { get; }
        
        public void PeekHeader(out NodeType type, out int length)
        {
            var position = Reader.Position;
            ReadHeader(out type, out length);
            Reader.Position = position;
        }
        
        public void ReadHeader(out NodeType type, out int length)
        {
            type = (NodeType) Reader.ReadByte();
            length = Reader.ReadInt32();
        }

        public string ReadKey() => Reader.ReadString(encoding);

        public void SkipNode()
        {
            ReadHeader(out _, out var length);
            Reader.Position += length;
        }
        
        public void CopyNodeTo(NodeWriter writer)
        {
            ReadHeader(out var type, out var length);

            writer.WriteHeader(type, length);
            writer.Writer.WriteWithoutLength(Reader.Buffer, (int) Reader.Position, length);

            Reader.Position += length;
        }
        
        public ISettingsNode ReadNode(string name)
        {
            ReadHeader(out var type, out var length);

            return type switch
            {
                NodeType.Object => ReadObject(name),
                NodeType.Array => ReadArray(name),
                NodeType.Value => ReadValue(name, length),
                NodeType.Delete => new DeleteNode(name),
                _ => throw new InvalidOperationException($"Unknown node type {type}")
            };
        }
        
        public ISettingsNode ReadNode(IEnumerator<string> path, string name)
        {
            if (!path.MoveNext())
                return ReadNode(name);
            
            ReadHeader(out var type, out _);
            
            if (type != NodeType.Object)
                return null;
            
            var enumerator = new KeyValuePairsEnumerator(this);
            while (enumerator.MoveNext())
            {
                if (Comparers.NodeName.Equals(path.Current, enumerator.CurrentKey))
                    return ReadNode(path, enumerator.CurrentKey);
                
                SkipNode();
            }

            return null;
        }
        
        private ObjectNode ReadObject(string name)
        {
            var enumerator = new KeyValuePairsEnumerator(this);
            var children = new List<ISettingsNode>(enumerator.Count);
            
            while (enumerator.MoveNext())
                children.Add(ReadNode(enumerator.CurrentKey));

            return new ObjectNode(name, children);
        }
        
        private ArrayNode ReadArray(string name)
        {
            var count = Reader.ReadInt32();

            var children = new List<ISettingsNode>(count);

            for (var i = 0; i < count; i++)
                children.Add(ReadNode(i.ToString()));

            return new ArrayNode(name, children);
        }
        
        private ValueNode ReadValue(string name, int length) =>
            new ValueNode(name, encoding.GetString(Reader.ReadByteArray(length)));
    }
}