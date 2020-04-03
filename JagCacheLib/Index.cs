using System;
using System.Drawing;
using System.IO;

namespace JagCacheLib
{
    public readonly struct IndexEntry
    {
        public IndexEntry(int id, int size, int offset)
        {
            Id = id;
            Size = size;
            Offset = offset;
        }

        public int Id { get; }
        public int Size { get; }
        public int Offset { get; }

        public override string ToString() =>
            $"Index Entry[id: {Id}, size: {Size}, offset: {Offset}]";
    }

    public class Index : IDisposable
    {
        public const byte Archive = 0;

        public const byte Size = 6;

        private static readonly byte[] Buffer = new byte[Size];

        public double Id { get; }

        public FileStream IndexFileStream { get; }

        public Index(int id, FileStream indexFileStream) => (Id, IndexFileStream) = (id, indexFileStream);

        public IndexEntry GetEntry(int entryId)
        {
            var pointer = entryId * Size;
            IndexFileStream.Seek(pointer, SeekOrigin.Begin);
            var read = IndexFileStream.Read(Buffer);

            if (read == 0)
            {
                throw new Exception($"Index entry {entryId} does not exist in the cache.");
            }

            int size = (Buffer[0] << 16) | (Buffer[1] << 8) | Buffer[2];
            int offset = (Buffer[3] << 16) | (Buffer[4] << 8) | Buffer[5];
            return new IndexEntry(entryId, size, offset);
        }

        public int GetFileCount() => (int) (IndexFileStream.Length / Size);

        public void Dispose()
        {
            IndexFileStream.Dispose();
        }
    }
}