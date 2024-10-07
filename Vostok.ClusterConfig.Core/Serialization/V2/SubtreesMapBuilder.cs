using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Vostok.Commons.Binary;
using Vostok.Commons.Collections;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal class SubtreesMapBuilder : NodeReader
    {
        public SubtreesMapBuilder(BinaryBufferReader reader, Encoding encoding, [CanBeNull] RecyclingBoundedCache<string, string> interningCache)
            : base(reader, encoding, interningCache)
        {
        }
        
        public Dictionary<string, ArraySegment<byte>> BuildMap()
        {
            var map = new Dictionary<string, ArraySegment<byte>>();
            
            VisitNode(map, new StringBuilder(512));

            return map;
        }

        public void VisitNode(Dictionary<string, ArraySegment<byte>> map, StringBuilder state)
        {
            ReadHeader(out var type, out var length);

            //(deniaa): вычитаем 5, это размер заголовка ноды.
            //(deniaa): Cоответственно к длине надо прибавить 5, чтобы получить нужные координаты поддерева.
            const int headerLength = 5;
            map[state.ToString()] = new ArraySegment<byte>(Reader.Buffer, (int)Reader.Position - headerLength, length + headerLength);
            
            switch (type)
            {
                case NodeType.Object:
                    VisitObject(length, map, state);
                    break;
                case NodeType.Array:
                    Reader.Position += length;
                    break;
                case NodeType.Value:
                    Reader.Position += length;
                    break;
                case NodeType.Delete:
                    Reader.Position += length;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown node type {type}");
            }
        }
        
        private void VisitObject(int length, Dictionary<string, ArraySegment<byte>> map, StringBuilder state)
        {
            var enumerator = new KeyValuePairsEnumerator(this);

            while (enumerator.MoveNext())
            {
                var nodeName = enumerator.CurrentKey;
                var offset = state.Length;
                var nameLength = nodeName.Length + 1;
                
                state.Append('/');
                state.Append(nodeName);
                
                VisitNode(map, state);
                
                state.Remove(offset, nameLength);
            }
        }
    }
}