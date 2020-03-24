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
        public const byte Size = 6;

        public double Id { get; }

        public FileStream IndexFileStream { get; }

        public Index(int id, FileStream indexFileStream) => (Id, IndexFileStream) = (id, indexFileStream);

        public IndexEntry GetEntry(int entryId)
        {
            var pointer = entryId * Size;
            // TODO check if the pointer is >= the index's size
            var buffer = new byte[Size];
            IndexFileStream.Seek(pointer, SeekOrigin.Begin);
            IndexFileStream.Read(buffer, 0, buffer.Length);
            int size = (buffer[0] << 16) | (buffer[1] << 8) | buffer[2];
            int offset = (buffer[3] << 16) | (buffer[4] << 8) | buffer[5];
            return new IndexEntry(entryId, size, offset);
        }

        public int GetFileCount() => (int) (IndexFileStream.Length / Size);

        public void Dispose()
        {
            IndexFileStream.Dispose();
        }
    }
}