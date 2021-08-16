using System.IO;

namespace Vostok.ClusterConfig.Core.Parsers
{
    internal interface IFileSizeLimiter
    {
        bool IsSizeAcceptable(FileInfo file, string zone);
    }
}