using System;
using FluentAssertions;
using NUnit.Framework;
using Vostok.ClusterConfig.Core.Serialization;
using Vostok.ClusterConfig.Core.Tests.Helpers;
using Vostok.Commons.Binary;
using Vostok.Commons.Collections;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Tests.Serialization;

[TestFixture]
public class TreeSerializerV2_Interning_Tests
{
    private TreeSerializerV2 serializer;
    private ISettingsNode tree;

    [Test]
    public void TestSetup()
    {
        var cache = new RecyclingBoundedCache<string, string>(100);
        serializer = new TreeSerializerV2(cache);

        tree = new TreeBuilder()
            .Add("foo/bar/baz", "00:01:00")
            .Add("foo/bar/baz2", "00:01:00")
            .Add("foo/bar/baz3", "00:01:00")
            .Add("one/two", "00:01:00")
            .Add("one/three/four", "00:01:00")
            .Build();
        
        
        var writer = new BinaryBufferWriter(64);
        serializer.Serialize(tree, writer);

        var deserializedTree = serializer.Deserialize(new ArraySegmentReader(new ArraySegment<byte>(writer.Buffer, 0, writer.Length)));
        deserializedTree.Should().Be(tree);

        ReferenceEquals(deserializedTree["foo"]["bar"]["baz"].Value, cache.Obtain("00:01:00", x => x)).Should().BeTrue();
        ReferenceEquals(deserializedTree["foo"]["bar"]["baz2"].Value, cache.Obtain("00:01:00", x => x)).Should().BeTrue();
        ReferenceEquals(deserializedTree["foo"]["bar"]["baz3"].Value, cache.Obtain("00:01:00", x => x)).Should().BeTrue();
        ReferenceEquals(deserializedTree["one"]["two"].Value, cache.Obtain("00:01:00", x => x)).Should().BeTrue();
        ReferenceEquals(deserializedTree["one"]["three"]["four"].Value, cache.Obtain("00:01:00", x => x)).Should().BeTrue();
    }
}