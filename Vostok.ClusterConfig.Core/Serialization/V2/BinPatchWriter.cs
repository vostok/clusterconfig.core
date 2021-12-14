using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Commons.Binary;

namespace Vostok.ClusterConfig.Core.Serialization.V2
{
    internal class BinPatchWriter : IDisposable
    {
        private readonly BinaryBufferWriter writer;

        // note (a.tolstov, 05.12.2021): Для оптимизации размера патча допустимы следующие переупорядочивания:
        // 1. Команды Skip и Write можно поменять местами
        // Это возможно делать, так как они мутируют независимые сущности:
        // - Skip сдвигает указатель в старом потоке настроек
        // - Write пишит данные в результирующий поток
        // А вот переставлять Copy с любой другой командой нельзя:
        // - С Skip они конфликтуют за указатель в старом потоке настроек: если не выполнить Skip, то будут скопированы не те данные
        // - C Write они конфликтуют за резултирующий поток, так как оба туда пишут
        
        private long skip;
        private long copy;
        private readonly List<(byte[] Source, long Offset, long Length)> write = new List<(byte[], long, long)>();

        public BinPatchWriter(BinaryBufferWriter writer)
        {
            this.writer = writer;
        }

        public void WriteNotDifferent(long length)
        {
            if (length == 0)
                return;
            
            if (skip != 0 || write.Any())
                Flush();

            copy += length;
        }

        public void WriteDelete(long length)
        {
            if (length == 0)
                return;
            
            if (copy != 0)
                Flush();

            skip += length;
        }

        public void WriteAppend(byte[] content, long offset, long length)
        {
            if (length == 0)
                return;
            
            if (copy != 0)
                Flush();

            write.Add((content, offset, length));
        }
        
        private void Flush()
        {
            if (skip != 0)
                WriteCommandHeader(PatchCommand.Skip, skip);
            
            if (copy != 0)
                WriteCommandHeader(PatchCommand.Copy, copy);

            if (write.Any())
            {
                WriteCommandHeader(PatchCommand.Write, write.Sum(c => c.Length));
                write.ForEach(w => writer.WriteWithoutLength(w.Source, (int)w.Offset, (int)w.Length));
            }

            skip = 0;
            copy = 0;
            write.Clear();
        }

        private void WriteCommandHeader(PatchCommand command, long length)
        {
            var lengthForDescriptor = length < 64 ? (byte)(length - 1) : 64;
            var descriptor = (byte)(((byte)command & 0b11_000000) | lengthForDescriptor);
            
            writer.Write(descriptor);
            
            if (length >= 64)
                writer.WriteVarlen((ulong) length);
        }

        public void Dispose() => Flush();
    }
}