using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.ClusterConfig.Core.Utils;
using Vostok.Commons.Binary;
using Vostok.Commons.Collections;
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

        public abstract void GetBinPatch(BinPatchContext context, BinPatchWriter writer);

        protected static bool EnsureSameType(BinPatchContext context, BinPatchWriter writer, out int oldLength, out int newLength)
        {
            var oldHeaderLength = ReadHeader(context.Old, out var oldType, out oldLength, out var oldMeta);
            var newHeaderLength = ReadHeader(context.New, out var newType, out newLength, out var newMeta);

            if (oldType != newType)
            {
                writer.WriteDelete(oldHeaderLength + oldLength);
                writer.WriteAppend(context.New.Buffer, context.New.Position - newHeaderLength, newHeaderLength + newLength);
                
                return false;
            }

            if (oldMeta != newMeta)
            {
                writer.WriteDelete(oldHeaderLength);
                writer.WriteAppend(context.New.Buffer, context.New.Position - newHeaderLength, newHeaderLength);
            }
            else
            {
                writer.WriteNotDifferent(oldHeaderLength);
            }
                    
            return true;
        }
        
        protected static void EnsureType(IBinaryReader reader, NodeType expected) => EnsureType(reader, expected, out _);
        protected static void EnsureType(IBinaryReader reader, NodeType expected, out int length) => EnsureType(reader, expected, out length, out _);
        protected static void EnsureType(IBinaryReader reader, NodeType expected, out int length, out byte meta)
        {
            ReadHeader(reader, out var type, out length, out _);
            if (type != expected)
                throw new InvalidOperationException($"{type} != {expected}");
        }
        
        protected void PeekHeader(IBinaryReader reader, out NodeType type, out int length)
        {
            var position = reader.Position;
            
            ReadHeader(reader, out type, out length, out _);

            reader.Position = position;
        }

        protected static int ReadHeader(IBinaryReader reader, out NodeType type, out int length, out byte meta)
        {
            var typeAndMeta = reader.ReadByte();
            type = (NodeType)(typeAndMeta & 0b11_000000);
            meta = (byte)(typeAndMeta & 0b00_1111111);
            length = reader.ReadInt32();

            return 1 + 4;
        }

        protected BinaryWriterExtensions.ContentLengthCounter Node(IBinaryWriter writer, NodeType type)
        {
            writer.Write((byte) type);
            return writer.AddContentLength();
        }
        
        protected void SkipNode(IBinaryReader reader)
        {
            ReadHeader(reader, out _, out var length, out _);
            reader.Position += length;
        }

        protected void CopyNode(BinaryBufferReader reader, IBinaryWriter writer)
        {
            ReadHeader(reader, out var type, out var length, out var meta);

            WriteHeader(writer, type, meta, length);
            writer.WriteWithoutLength(reader.Buffer, (int) reader.Position, length);

            reader.Position += length;
        }
        
        private static void WriteHeader(IBinaryWriter writer, NodeType type, byte meta, int length)
        {
            writer.Write((byte)(((byte) type) | (meta & 0b00_111111)));
            writer.Write(length);
        }
        
        protected enum NodeType : byte
        {
            Delete = 0b00_000000,
            Value =  0b01_000000,
            Array =  0b10_000000,
            Object = 0b11_000000,
        }
    }
}