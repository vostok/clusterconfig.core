using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Binary;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.ClusterConfig.Core.Serialization
{
    internal interface ITreeSerializerV2 : ITreeSerializer
    {
        [CanBeNull] ISettingsNode Deserialize([NotNull] byte[] tree);

        [CanBeNull] ISettingsNode Deserialize([NotNull] byte[] tree, [NotNull] IEnumerable<string> path);
        
        void ApplyPatch([NotNull] byte[] settings, [NotNull] byte[] patch, [NotNull] IBinaryWriter result);
    }
}