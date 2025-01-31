using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using Vostok.Commons.Binary;
using Vostok.Commons.Collections;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal class SubtreesMapBuilder : NodeReader
    {
        [CanBeNull] private readonly RecyclingBoundedCache<string, string> interningCache;

        public SubtreesMapBuilder(ArraySegmentReader reader, Encoding encoding, [CanBeNull] RecyclingBoundedCache<string, string> interningCache)
            : base(reader, encoding, null)
        {
            this.interningCache = interningCache;
        }
        
        public Dictionary<string, ArraySegment<byte>> BuildMap()
        {
            if (Reader.BytesRemaining == 0)
                return new Dictionary<string, ArraySegment<byte>>(0);
            
            var map = new Dictionary<string, ArraySegment<byte>>(Comparers.NodeName);
            
            VisitNode(map, new StringBuilder(512));

            return map;
        }

        public void VisitNode(Dictionary<string, ArraySegment<byte>> map, StringBuilder state)
        {
            ReadHeader(out var type, out var length);

            //(deniaa): Substract 5 as a size of the node header to point to its start.
            //(deniaa): And we have to add 5 back to the subtree length.
            const int headerLength = 5;
            map[Intern(state.ToString())] = new ArraySegment<byte>(
                Reader.Segment.Array!,
                (int)Reader.ArrayPosition - headerLength,
                length + headerLength);
            
            switch (type)
            {
                case NodeType.Object:
                    VisitObject(map, state);
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
        
        private void VisitObject(Dictionary<string, ArraySegment<byte>> map, StringBuilder state)
        {
            var enumerator = new KeyValuePairsEnumerator(this);

            while (enumerator.MoveNext())
            {
                var nodeName = enumerator.CurrentKey;
                var offset = state.Length;
                var nameLength = nodeName.Length;

                if (state.Length > 0)
                {
                    //(deniaa): We don't want to add leading slash
                    state.Append('/');
                    nameLength += 1;
                }

                state.Append(nodeName);
                
                VisitNode(map, state);
                
                state.Remove(offset, nameLength);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string Intern(string value)
        {
            return interningCache == null || value == null 
                ? value 
                : interningCache.Obtain(value, x => x);
        }
    }
}