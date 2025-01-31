using JetBrains.Annotations;
using Vostok.Commons.Binary;

namespace Vostok.ClusterConfig.Core.Patching
{
    internal interface IBinaryPatcher
    {
        void ApplyPatch([NotNull] ArraySegmentReader settings, [NotNull] ArraySegmentReader patch, [NotNull] IBinaryWriter result);
    }
}