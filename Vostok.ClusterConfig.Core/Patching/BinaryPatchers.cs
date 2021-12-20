using Vostok.ClusterConfig.Core.Serialization;

namespace Vostok.ClusterConfig.Core.Patching
{
    internal static class BinaryPatchers
    {
        public static readonly IBinaryPatcher V2 = new TreeSerializerV2();
    }
}