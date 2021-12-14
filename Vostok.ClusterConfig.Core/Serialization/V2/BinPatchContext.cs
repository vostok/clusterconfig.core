using Vostok.Commons.Binary;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal struct BinPatchContext
    {
        public readonly BinaryBufferReader Old;
        public readonly BinaryBufferReader New;

        private readonly IBinaryWriter patch;

        public readonly long OldNodeBegin;
        public readonly long NewNodeBegin;

        public readonly long? OldEquals;
            
        public BinPatchContext(
            BinaryBufferReader old,
            BinaryBufferReader @new,
            IBinaryWriter patch,
            long oldNodeBegin,
            long newNodeBegin,
            long? oldEquals)
        {
            Old = old;
            New = @new;
            this.patch = patch;
            OldNodeBegin = oldNodeBegin;
            NewNodeBegin = newNodeBegin;
            OldEquals = oldEquals;
        }

        public BinPatchContext Write()
        {
                
        }
            
        private void Write
    }
}