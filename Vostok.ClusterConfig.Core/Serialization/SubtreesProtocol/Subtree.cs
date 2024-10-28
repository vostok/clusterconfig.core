using System;
using System.IO;
using System.IO.Compression;

namespace Vostok.ClusterConfig.Core.Serialization.SubtreesProtocol
{
    internal class Subtree
    {
        public Subtree(bool wasModified, bool hasSubtree, bool isPatch, bool isCompressed, ArraySegment<byte> content)
        {
            WasModified = wasModified;
            HasSubtree = hasSubtree;
            IsPatch = isPatch;
            IsCompressed = isCompressed;
            Content = content;
        }

        public void DecompressIfNeeded()
        {
            if (!IsCompressed)
                return;
            
            using var decompressedTreeStream = new MemoryStream(Content.Count);
                            
            using (var treeSegmentStream = new MemoryStream(Content.Array!, Content.Offset, Content.Count))
            using (var gzipStream = new GZipStream(treeSegmentStream, CompressionMode.Decompress))
            {
                gzipStream.CopyTo(decompressedTreeStream);
            }

            if (!decompressedTreeStream.TryGetBuffer(out var segment))
            {
                throw new Exception("Bug in code");
            }

            Content = segment;
            IsCompressed = false;
        }

        public bool WasModified { get; set; }
        public bool HasSubtree { get; set; }
        public bool IsPatch { get; set; }
        public bool IsCompressed { get; set; }
        public ArraySegment<byte> Content { get; set; }
    }
}