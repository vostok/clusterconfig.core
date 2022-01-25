using JetBrains.Annotations;
using Vostok.Commons.Binary;

namespace Vostok.ClusterConfig.Core.Patching
{
    internal interface IBinaryPatcher
    {
        void ApplyPatch([NotNull] BinaryBufferReader settings, [NotNull] BinaryBufferReader patch, [NotNull] IBinaryWriter result);
    }
}