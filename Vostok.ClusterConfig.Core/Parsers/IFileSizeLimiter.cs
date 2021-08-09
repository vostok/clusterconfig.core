using System.IO;

namespace Vostok.ClusterConfig.Core.Parsers
{
    public interface IFileSizeLimiter
    {
        bool IsSizeAcceptable(FileInfo file, string zone);
    }
}