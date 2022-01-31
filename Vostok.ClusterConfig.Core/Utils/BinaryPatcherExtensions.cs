using System;
using Vostok.ClusterConfig.Core.Patching;
using Vostok.Commons.Binary;

namespace Vostok.ClusterConfig.Core.Utils
{
    internal static class BinaryPatcherExtensions
    {
        public static byte[] ApplyPatch(this IBinaryPatcher patcher, byte[] old, byte[] patch)
        {
            var writer = new BinaryBufferWriter(4096);

            patcher.ApplyPatch(new BinaryBufferReader(old, 0), new BinaryBufferReader(patch, 0), writer);

            var result = new byte[writer.Length];

            Buffer.BlockCopy(writer.Buffer, 0, result, 0, result.Length);

            return result;
        }
    }
}