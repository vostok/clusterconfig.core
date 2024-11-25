using System;

namespace Vostok.ClusterConfig.Core.Serialization.SubtreesProtocol
{
    internal class SubtreeRequest
    {
        public SubtreeRequest(string prefix, DateTime? version, bool forceFullUpdate)
        {
            Prefix = prefix;
            Version = version;
            ForceFullUpdate = forceFullUpdate;
        }

        public string Prefix { get; }
        public DateTime? Version { get; }
        public bool ForceFullUpdate { get; }
    }
}