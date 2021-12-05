using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization
{
    internal interface ITreeSerializerV1 : ITreeSerializer
    {
        [NotNull]
        ISettingsNode Deserialize([NotNull] IBinaryReader reader);

        [CanBeNull]
        ISettingsNode Deserialize([NotNull] IBinaryReader reader, IEnumerable<string> path);
    }
}
