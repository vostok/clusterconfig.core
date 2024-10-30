using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using Vostok.ClusterConfig.Core.Serialization.SubtreesProtocol;
using Vostok.Commons.Binary;

namespace Vostok.ClusterConfig.Core.Tests.Serialization;

[TestFixture]
public class SubtreesResponseSerializer_Tests
{
    [TestCase(true)]
    [TestCase(false)]
    public void Should_serialize_and_deserialize_single_subtree(bool autoDecompress)
    {
        var subtree = new Subtree(true, true, false, false, GenerateRandom());
        var subtrees = new Dictionary<string, Subtree> {{"single/subtree", subtree}};
        
        SerializeDeserializeCheck(subtrees, autoDecompress);
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public void Should_serialize_and_deserialize_several_subtrees(bool autoDecompress)
    {
        var subtrees = new Dictionary<string, Subtree>
        {
            {"several/subtree1", new Subtree(true, true, false, false, GenerateRandom())},
            {"several/subtree2", new Subtree(true, false, false, false, default)},
            {"several/subtree3", new Subtree(false, false, false, false, default)},
            {"/", new Subtree(true, true, false, true, GenerateRandom(true))},
            {"several/subtree4", new Subtree(true, true, true, false, GenerateRandom())},
        };
        
        SerializeDeserializeCheck(subtrees, autoDecompress);
    }

    private static ArraySegment<byte> GenerateRandom(bool compressed = false)
    {
        var rnd = new Random();
        var count = rnd.Next(1024);
        var array = new byte[count];
        rnd.NextBytes(array);

        var segment = new ArraySegment<byte>(array);
        return !compressed ? segment : Compress(segment);
    }

    private static ArraySegment<byte> Compress(ArraySegment<byte> array)
    {
        using var compressedTreeStream = new MemoryStream(42);
        using (var gzipStream = new GZipStream(compressedTreeStream, CompressionMode.Compress, true))
        {
            gzipStream.Write(array.Array!, array.Offset, array.Count);
            gzipStream.Flush();
        }

        if (!compressedTreeStream.TryGetBuffer(out var segment))
        {
            throw new Exception("Bug in code");
        }

        return segment;
    }

    private static void SerializeDeserializeCheck(Dictionary<string, Subtree> subtrees, bool autoDecompress)
    {
        var writer = new BinaryBufferWriter(1024);
        
        SubtreesResponseSerializer.Serialize(writer, subtrees);

        var reader = new BinaryBufferReader(writer.Buffer, 0);

        var deserializedSubtrees = SubtreesResponseSerializer.Deserialize(reader, Encoding.UTF8, autoDecompress);

        deserializedSubtrees.Count.Should().Be(subtrees.Count);
        foreach (var pair in subtrees)
        {
            var original = pair.Value;
            var deserialized = deserializedSubtrees[pair.Key];
            
            deserialized.HasSubtree.Should().Be(original.HasSubtree);
            if (autoDecompress)
                deserialized.IsCompressed.Should().BeFalse();
            else
                deserialized.IsCompressed.Should().Be(original.IsCompressed);
            deserialized.IsPatch.Should().Be(original.IsPatch);
            deserialized.WasModified.Should().Be(original.WasModified);
            if (original.HasSubtree)
            {
                //(deniaa): As we used compressed content in original sometimes, we need to decompress original if autoDecompress if true to check content
                if (autoDecompress)
                    original.DecompressIfNeeded();
                
                deserialized.Content.Should().BeEquivalentTo(original.Content);
            }
        }
    }
}