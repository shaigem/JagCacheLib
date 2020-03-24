using System;
using System.Collections;
using System.IO;
using System.Text;

namespace JagCacheLib
{
    public class Cache
    {
        private const string DataFileName = "main_file_cache.dat2";
        private const string IndexFilePrefixName = "main_file_cache.idx";
        private const byte MasterIndexId = 255;
        
        private readonly FileStream _mainDataFileStream;
        private readonly FileStream _masterIndexFileStream;
        public Index[] Indices { get; }
        public Index MasterIndex { get; }
        
        public Cache(string path)
        {
            // TODO check if exists
            var mainDataPath = Path.Combine(path, DataFileName);
            var masterIndexPath = Path.Combine(path, $"{IndexFilePrefixName}{MasterIndexId}");
            _mainDataFileStream = OpenFile(mainDataPath);
            _masterIndexFileStream = OpenFile(masterIndexPath);
            MasterIndex = new Index(MasterIndexId, _masterIndexFileStream);
            var indexCount = MasterIndex.GetFileCount();
            Indices = new Index[indexCount];
            for (var id = 0; id < indexCount; id++)
            {
                var indexPath = Path.Combine(path, $"{IndexFilePrefixName}{id}");
                if (!File.Exists(indexPath))
                {
                    throw new FileNotFoundException($"Problem loading index files. {indexPath} cannot be found.");
                }
                var indexStream = OpenFile(indexPath);
                var index = new Index(id, indexStream);
                Indices[id] = index;
            }
            // TODO provide custom fileaccess and filemode
            static FileStream OpenFile(string path) => File.Open(path, FileMode.Open, FileAccess.ReadWrite);
        }
    }
}