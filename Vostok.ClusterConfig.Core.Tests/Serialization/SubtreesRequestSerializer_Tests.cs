using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.ClusterConfig.Core.Serialization.SubtreesProtocol;
using Vostok.Commons.Binary;

namespace Vostok.ClusterConfig.Core.Tests.Serialization;

[TestFixture]
public class SubtreesRequestSerializer_Tests
{
    [Test]
    public void Should_serialize_and_deserialize_list()
    {
        var requests = new List<(string prefix, DateTime? version, bool forceFullUpdate)>
        {
            ("/", DateTime.UtcNow, true),
            ("", DateTime.UtcNow - 2.Minutes(), false),
            ("/unseen/subtree", null, false),
            ("/seen/subtree", DateTime.UtcNow - 5.Minutes(), false),
        };
        SerializeDeserializeCheck(requests);
    }
    
    [Test]
    public void Should_serialize_and_deserialize_single_item()
    {
        var requests = new List<(string prefix, DateTime? version, bool forceFullUpdate)>
        {
            ("/some/deep/subtree", DateTime.UtcNow - 5.Minutes(), false),
        };
        SerializeDeserializeCheck(requests);
    }

    private static void SerializeDeserializeCheck(List<(string prefix, DateTime? version, bool forceFullUpdate)> subtreesRequest)
    {
        var writer = new BinaryBufferWriter(1024);

        SubtreesRequestSerializer.Serialize(writer, subtreesRequest);

        var reader = new BinaryBufferReader(writer.Buffer, 0);

        var deserializedSubtrees = SubtreesRequestSerializer.Deserialize(reader, Encoding.UTF8);

        deserializedSubtrees.Should().BeEquivalentTo(subtreesRequest);
    }
}