using System;
using Vostok.Commons.Binary;

namespace Vostok.ClusterConfig.Core.Utils
{
    internal static class BinaryWriterExtensions
    {
        public static void Write(this IBinaryWriter writer, int value, long position)
        {
            var current = writer.Position;

            writer.Position = position;
            writer.Write(value);

            writer.Position = current;
        }

        public static IntVariable WriteIntVariable(this IBinaryWriter writer)
        {
            var position = writer.Position;
            writer.Write(int.MaxValue); // reserve 32 bits for variable
            return new IntVariable(writer, position);
        }

        // note (a.tolstov, 03.12.2021): Returns ContentLength instead IDisposable for prevent boxing 
        public static ContentLengthCounter AddContentLength(this IBinaryWriter writer)
        {
            var length = writer.WriteIntVariable();
            return new ContentLengthCounter(writer, writer.Position, length);
        }
        
        public readonly struct ContentLengthCounter : IDisposable
        {
            private readonly IBinaryWriter writer;
            private readonly long startPosition;
            private readonly IntVariable lengthVariable;
            
            public ContentLengthCounter(IBinaryWriter writer, long startPosition, IntVariable lengthVariable)
            {
                this.writer = writer;
                this.startPosition = startPosition;
                this.lengthVariable = lengthVariable;
            }

            public void Dispose()
            {
                lengthVariable.Set((int)(writer.Position - startPosition));
            }
        }
    }
}