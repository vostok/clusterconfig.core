using Vostok.Commons.Binary;

namespace Vostok.ClusterConfig.Core.Utils
{
    internal readonly struct IntVariable
    {
        private readonly IBinaryWriter writer;
        private readonly long position;

        public IntVariable(IBinaryWriter writer, long position)
        {
            this.writer = writer;
            this.position = position;
        }

        public void Set(int value) => writer.Write(value, position);
    }
}