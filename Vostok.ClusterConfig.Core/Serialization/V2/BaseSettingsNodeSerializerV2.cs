using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.ClusterConfig.Core.Utils;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal abstract class BaseSettingsNodeSerializerV2<TNode>
        where TNode : ISettingsNode
    {
        public abstract void Serialize([NotNull] TNode node, [NotNull] IBinaryWriter writer);

        [NotNull] public abstract TNode Deserialize([NotNull] IBinaryReader reader, string name);
        
        [CanBeNull] public abstract ISettingsNode Deserialize([NotNull] IBinaryReader reader, IEnumerator<string> path, string name);

        public abstract void ApplyPatch([NotNull] BinaryBufferReader settings, [NotNull] BinaryBufferReader patch, [NotNull] IBinaryWriter result);

        protected void EnsureType(IBinaryReader reader, NodeType expected) => EnsureType(reader, expected, out _);
        protected void EnsureType(IBinaryReader reader, NodeType expected, out int length)
        {
            ReadHeader(reader, out var type, out length);
            if (type != expected)
                throw new InvalidOperationException($"{type} != {expected}");
        }
        
        protected void PeekHeader(IBinaryReader reader, out NodeType type, out int length)
        {
            var position = reader.Position;
            
            ReadHeader(reader, out type, out length);

            reader.Position = position;
        }

        protected void ReadHeader(IBinaryReader reader, out NodeType type, out int length)
        {
            type = (NodeType) reader.ReadByte();
            length = reader.ReadInt32();
        }

        protected BinaryWriterExtensions.ContentLengthCounter BeginNode(IBinaryWriter writer, NodeType type)
        {
            writer.Write((byte) type);
            return writer.AddContentLength();
        }
        
        protected void SkipNode(IBinaryReader reader)
        {
            ReadHeader(reader, out _, out var length);
            reader.Position += length;
        }

        protected void CopyNode(BinaryBufferReader reader, IBinaryWriter writer)
        {
            ReadHeader(reader, out var type, out var length);

            WriteHeader(writer, type, length);
            writer.WriteWithoutLength(reader.Buffer, (int) reader.Position, length);

            reader.Position += length;
        }
        
        private static void WriteHeader(IBinaryWriter writer, NodeType type, int length)
        {
            writer.Write((byte) type);
            writer.Write(length);
        }
        
        protected enum NodeType : byte
        {
            Object = 1,
            Array = 2,
            Value = 3,
            Delete = 4
        }
    }
}