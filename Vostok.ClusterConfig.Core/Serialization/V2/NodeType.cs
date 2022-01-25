namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal enum NodeType : byte
    {
        Object = 1,
        Array = 2,
        Value = 3,
        Delete = 4
    }
}