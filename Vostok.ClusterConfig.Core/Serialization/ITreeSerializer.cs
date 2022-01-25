using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization
{
    internal interface ITreeSerializer
    {
        void Serialize([NotNull] ISettingsNode tree, [NotNull] IBinaryWriter writer);
        
        [CanBeNull] ISettingsNode Deserialize([NotNull] BinaryBufferReader tree);

        [CanBeNull] ISettingsNode Deserialize([NotNull] BinaryBufferReader tree, [NotNull] IEnumerable<string> path);
    }
}