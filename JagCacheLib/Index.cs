using System;
using System.IO;

namespace JagCacheLib
{
    public readonly ref struct IndexEntry
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

        public override string ToString()
        {
            return $"Index Entry[id: {Id}, size: {Size}, offset: {Offset}]";
        }
    }

    public class Index : IDisposable
    {
        public const byte Size = 6;

        private static readonly byte[] Buffer = new byte[Size];

        public Index(int id, FileStream indexFileStream)
        {
            (Id, IndexFileStream) = (id, indexFileStream);
        }

        public double Id { get; }
        public FileStream IndexFileStream { get; }

        public void Dispose()
        {
            IndexFileStream.Dispose();
        }

        public IndexEntry GetEntry(int entryId)
        {
            var pointer = entryId * Size;
            IndexFileStream.Seek(pointer, SeekOrigin.Begin);
            var read = IndexFileStream.Read(Buffer);

            if (read == 0) throw new Exception($"Index entry {entryId} does not exist in the cache.");

            var size = (Buffer[0] << 16) | (Buffer[1] << 8) | Buffer[2];
            var offset = (Buffer[3] << 16) | (Buffer[4] << 8) | Buffer[5];
            return new IndexEntry(entryId, size, offset);
        }

        public int GetFileCount()
        {
            return (int) (IndexFileStream.Length / Size);
        }
    }
}