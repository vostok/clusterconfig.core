using System.Collections.Generic;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Tests.Helpers
{
    public static class NodeExtensions
    {
        public static ObjectNode WithName(this ObjectNode node, string name) =>
            new ObjectNode(name, node.Children);

        public static IEnumerable<ISettingsNode> EnumerateNodes(this ISettingsNode node)
        {
            yield return node;
            
            foreach (var child in node.Children)
            foreach (var n in child.EnumerateNodes())
                yield return n;
        }
    }
}