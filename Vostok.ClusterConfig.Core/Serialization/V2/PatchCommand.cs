namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal enum PatchCommand : byte
    {
        Skip =  0b00_000000,
        Copy =  0b01_000000,
        Write = 0b10_000000,
    }
}