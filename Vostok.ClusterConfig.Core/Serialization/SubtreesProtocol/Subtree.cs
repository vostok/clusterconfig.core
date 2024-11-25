using System;
using System.IO;
using System.IO.Compression;

namespace Vostok.ClusterConfig.Core.Serialization.SubtreesProtocol
{
    internal class Subtree
    {
        public Subtree(bool wasModified, bool subtreeExists, bool isPatch, bool isCompressed, ArraySegment<byte> content)
        {
            WasModified = wasModified;
            SubtreeExists = subtreeExists;
            IsPatch = isPatch;
            IsCompressed = isCompressed;
            Content = content;
        }

        public bool WasModified { get; set; }
        public bool SubtreeExists { get; set; }
        public bool IsPatch { get; set; }
        public bool IsCompressed { get; set; }
        public ArraySegment<byte> Content { get; set; }

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
    }
}