using System.Drawing;
using System.IO;

namespace JagCacheLib
{
    public readonly struct Index
    {
        public const byte Size = 6;
        
        public double Id { get; }
        
        public FileStream IndexFileStream { get; }
        
        public Index(int id, FileStream indexFileStream) => (Id, IndexFileStream) = (id, indexFileStream);
        
        public int GetFileCount() => (int) (IndexFileStream.Length / Size);
    }
}