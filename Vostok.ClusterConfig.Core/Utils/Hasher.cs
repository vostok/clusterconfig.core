using System;
using System.Security.Cryptography;

namespace Vostok.ClusterConfig.Core.Utils
{
    public static class Hasher
    {
        public static byte[] GetSha256(this byte[] buffer) => buffer.GetSha256(0, buffer.Length);
        public static byte[] GetSha256(this byte[] buffer, int offset, int length)
        {
            using (var sha256 = SHA256.Create())
                return sha256.ComputeHash(buffer, offset, length);
        }

        public static string GetSha256Str(this byte[] buffer) => buffer.GetSha256Str(0, buffer.Length);
        public static string GetSha256Str(this byte[] buffer, int offset, int length) => Convert.ToBase64String(buffer.GetSha256(offset, length));
    }
}