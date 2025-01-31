using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization
{
    internal interface ITreeSerializer
    {
        void Serialize([NotNull] ISettingsNode tree, [NotNull] IBinaryWriter writer);
        
        [CanBeNull] ISettingsNode Deserialize([NotNull] ArraySegmentReader tree);

        [CanBeNull] ISettingsNode Deserialize([NotNull] ArraySegmentReader tree, [NotNull] IEnumerable<string> path, [CanBeNull] string rootName);
    }
}