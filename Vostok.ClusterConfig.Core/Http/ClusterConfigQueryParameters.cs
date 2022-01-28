namespace Vostok.ClusterConfig.Core.Http
{
    internal static class ClusterConfigQueryParameters
    {
        public const string ForceFullKey = "forceFull";

        public static class ForceFullReason
        {
            public const string ProtocolChanged = "protocolChanged";
            public const string NoPrevious = "noPrevious";   
        }
    }
}