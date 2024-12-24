using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Vostok.ClusterConfig.Core.Patching;
using Vostok.ClusterConfig.Core.Serialization.V2;
using Vostok.ClusterConfig.Core.Utils;
using Vostok.Commons.Binary;
using Vostok.Commons.Collections;
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
        private readonly Encoding encoding;
        private readonly RecyclingBoundedCache<string, string> interningCache;

        public TreeSerializerV2() : this(Encoding.UTF8, null) { }
        public TreeSerializerV2(RecyclingBoundedCache<string, string> interningCache) : this(Encoding.UTF8, interningCache) { }
        public TreeSerializerV2(Encoding encoding, RecyclingBoundedCache<string, string> interningCache)
        {
            this.encoding = encoding;
            this.interningCache = interningCache;
        }

        public void Serialize([CanBeNull] ISettingsNode tree, IBinaryWriter writer)
        {
            if (tree != null)
                new NodeWriter(writer, encoding).WriteNode(tree);
        }

        public ISettingsNode Deserialize(ArraySegmentReader tree)
        {
            return tree.BytesRemaining > 0 ? new NodeReader(tree, encoding, interningCache).ReadNode(null) : null;
        }

        public ISettingsNode Deserialize(ArraySegmentReader tree, IEnumerable<string> path, [CanBeNull] string rootName)
        {
            using var pathEnumerator = path.GetEnumerator();
            
            return tree.BytesRemaining > 0 ? new NodeReader(tree, encoding, interningCache).ReadNode(pathEnumerator, rootName) : null;
        }

        public void ApplyPatch(ArraySegmentReader settings, ArraySegmentReader patch, IBinaryWriter result)
        {
            if (patch.BytesRemaining == 0)
            {
                result.WriteWithoutLength(settings.Segment.Array!, (int)settings.ArrayPosition, (int) settings.BytesRemaining);
            }
            else if (settings.BytesRemaining == 0)
            {
                result.WriteWithoutLength(patch.Segment.Array!, (int)patch.ArrayPosition, (int) patch.BytesRemaining);
            }
            else
            {
                ApplyPatch(new NodeReader(settings, encoding, interningCache), new NodeReader(patch, encoding, interningCache), new NodeWriter(result, encoding));
            }
        }

        private void ApplyPatch(NodeReader settings, NodeReader patch, NodeWriter result)
        {
            settings.PeekHeader(out var settingsType, out _);
            patch.PeekHeader(out var patchType, out _);

            if (patchType == NodeType.Delete)
            {
                settings.SkipNode();
                patch.SkipNode();
            }
            else if (settingsType != patchType)
            {
                settings.SkipNode();
                patch.CopyNodeTo(result);
            }
            else
            {
                switch (patchType)
                {
                    case NodeType.Object:
                        MergeObject(settings, patch, result);
                        break;
                    case NodeType.Array:
                    case NodeType.Value:
                        settings.SkipNode();
                        patch.CopyNodeTo(result);
                        break;
                    default: 
                        throw new InvalidOperationException($"Unknown node type {patchType}");
                }
            }
        }
        
        private void MergeObject(NodeReader settings, NodeReader patch, NodeWriter result)
        {
            settings.ReadHeader(out _, out _);
            patch.ReadHeader(out _, out _);

            using var __ = result.WriteHeader(NodeType.Object);
            
            var childCountVariable = result.Writer.WriteIntVariable();

            var childCount = MergeChildren(new KeyValuePairsEnumerator(settings), new KeyValuePairsEnumerator(patch), result);
                
            childCountVariable.Set(childCount);
        }

        private int MergeChildren(KeyValuePairsEnumerator settings, KeyValuePairsEnumerator patch, NodeWriter result)
        {
            var count = 0;

            var settingsKey = default(string);
            var patchKey = default(string);

            while (true)
            {
                settingsKey ??= settings.MoveNext() ? settings.CurrentKey : null;
                patchKey ??= patch.MoveNext() ? patch.CurrentKey : null;

                if (settingsKey == null && patchKey == null)
                    break;
                
                if (patchKey == null)
                {
                    CopyChildFromSettings();
                    continue;
                }

                if (settingsKey == null)
                {
                    CopyChildFromPatch();
                    continue;
                }

                var comparison = Comparers.NodeNameComparer.Compare(settingsKey, patchKey);
                if (comparison == 0) MergeChildrenFromSettingsAndPatch();
                else if (comparison < 0) CopyChildFromSettings();
                else CopyChildFromPatch();
            }
            
            return count;

            void CopyChildFromSettings()
            {
                result.WriteKey(settingsKey);
                settings.Reader.CopyNodeTo(result);
                count++;
                settingsKey = null;
            }

            void CopyChildFromPatch()
            {
                result.WriteKey(patchKey);
                patch.Reader.CopyNodeTo(result);
                count++;
                patchKey = null;
            }
            
            void MergeChildrenFromSettingsAndPatch()
            {
                patch.Reader.PeekHeader(out var patchType, out _);
                if (patchType == NodeType.Delete)
                {
                    settings.Reader.SkipNode();
                    patch.Reader.SkipNode();
                }
                else
                {
                    result.WriteKey(patchKey);
                    ApplyPatch(settings.Reader, patch.Reader, result);
                    count++;
                }

                settingsKey = null;
                patchKey = null;
            }
        }
    }
}