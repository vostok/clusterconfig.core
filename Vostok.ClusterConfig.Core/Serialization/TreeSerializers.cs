namespace Vostok.ClusterConfig.Core.Serialization
{
    internal static class TreeSerializers
    {
        public static readonly ITreeSerializer V1 = new TreeSerializerV1();
        public static readonly ITreeSerializerV2 V2 = new TreeSerializerV2();
    }
}
