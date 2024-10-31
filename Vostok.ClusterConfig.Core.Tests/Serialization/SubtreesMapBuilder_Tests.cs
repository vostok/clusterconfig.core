using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using Vostok.ClusterConfig.Core.Serialization;
using Vostok.ClusterConfig.Core.Serialization.V2;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Tests.Serialization;

[TestFixture]
internal class SubtreesMapBuilder_Tests
{
    [Test]
    public void SerializeWithMap_Test()
    {
        var tree = new ObjectNode(new ISettingsNode[]
        {
            new ValueNode("ValueUnderRoot", "AwesomeValue"),
            new ObjectNode("ObjectUnderRoot", new ISettingsNode[]
            {
                new ArrayNode("ArrayOfValues", new[]
                {
                    new ValueNode("0", "12"),
                    new ValueNode("1", "235")
                }),
                new ArrayNode("ArrayOfValuesWithSameKeys", new ISettingsNode[]
                {
                    new ValueNode("0", "12"),
                    new ValueNode("1", "12"),
                    new ValueNode("2", "44"),
                    new ValueNode("3", "55")
                }),
                new ObjectNode("SomeObject", new[]
                {
                    new ObjectNode("NestedObject", new[]
                    {
                        new ValueNode("value5", "qqq"),
                        new ValueNode("value6", "www"),
                    })
                })
            }),
        });

        var writer = new BinaryBufferWriter(64);

        var serializer = new TreeSerializerV2();
        serializer.Serialize(tree, writer);

        var subtreesMapBuilder = new SubtreesMapBuilder(new BinaryBufferReader(writer.Buffer, 0), Encoding.UTF8, null);
        var map = subtreesMapBuilder.BuildMap();
            
        foreach (var pair in map.OrderByDescending(x => x.Value.Offset))
        {
            var coordinates = pair.Key.Split(new []{"/"}, StringSplitOptions.RemoveEmptyEntries);
            var node = (ISettingsNode)tree;
            foreach (var coordinate in coordinates)
                node = node[coordinate];

            var nodeReader = new NodeReader(new BinaryBufferReader(pair.Value.Array, pair.Value.Offset), Encoding.UTF8, null);
            var deserializedNode = nodeReader.ReadNode(coordinates.LastOrDefault());

            deserializedNode.Equals(node).Should().BeTrue();
        }
    }
}