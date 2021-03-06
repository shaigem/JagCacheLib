﻿using System;
using System.Collections.Generic;
using System.IO;

namespace JagCacheLib
{
    public class Cache : IDisposable
    {
        private const string DataFileName = "main_file_cache.dat2";
        private const string IndexFilePrefixName = "main_file_cache.idx";

        private const byte BlockHeaderSize = 8;
        private const byte BlockHeaderExtendedSize = 10;

        private const ushort BlockChunkSize = 512;
        private const ushort BlockChunkExtendedSize = 510;

        private const ushort TotalBlockSize = 520;

        private static readonly byte[] BlockBuffer = new byte[TotalBlockSize];
        private readonly Index?[] _indices;

        private readonly FileStream _mainDataFileStream;

        private readonly Index _mainDescriptorIndex;

        public Cache(string path)
        {
            // TODO better exception handling

            var mainDataPath = Path.Combine(path, DataFileName);
            if (!File.Exists(mainDataPath)) throw new FileNotFoundException($"{mainDataPath} cannot be found.");

            _mainDataFileStream = OpenFile(mainDataPath);

            var mainDescriptorIndexPath = Path.Combine(path, $"{IndexFilePrefixName}255");
            if (!File.Exists(mainDescriptorIndexPath))
                throw new FileNotFoundException($"{mainDescriptorIndexPath} cannot be found.");

            _mainDescriptorIndex = new Index(255, OpenFile(mainDescriptorIndexPath));

            var indexCount = _mainDescriptorIndex.GetFileCount();
            _indices = new Index[indexCount];
            for (var id = 0; id < indexCount; id++)
            {
                var indexPath = Path.Combine(path, $"{IndexFilePrefixName}{id}");
                if (File.Exists(indexPath) && !Directory.Exists(indexPath))
                {
                    var indexStream = OpenFile(indexPath);
                    var index = new Index(id, indexStream);
                    _indices[id] = index;
                }
            }

            // TODO provide custom fileaccess and filemode
            static FileStream OpenFile(string path)
            {
                return File.Open(path, FileMode.Open, FileAccess.ReadWrite);
            }
        }

        private static ReadOnlySpan<byte> BlockBufferSpan => BlockBuffer;

        public void Dispose()
        {
            _mainDataFileStream.Dispose();
            _mainDescriptorIndex.Dispose();
            foreach (var index in _indices) index?.Dispose();
        }

        public byte[] Read(int type, int file)
        {
            var index = GetIndex(type);
            var indexEntry = index.GetEntry(file);
            var remainingBytes = indexEntry.Size;
            var block = indexEntry.Offset;
            var currentSequence = 0;

            var large = file > ushort.MaxValue;
            int blockHeaderSize = large ? BlockHeaderExtendedSize : BlockHeaderSize;
            int blockChunkSize = large ? BlockChunkExtendedSize : BlockChunkSize;

            var data = new byte[indexEntry.Size];

            var dataReadIndex = 0;

            while (remainingBytes > 0)
            {
                if (remainingBytes <= 0) continue;

                _mainDataFileStream.Seek(block * TotalBlockSize, SeekOrigin.Begin);
                var read = _mainDataFileStream.Read(BlockBuffer);
                if (read == 0)
                    throw new Exception(
                        $"Reached the end of file while trying to read the data block position of {block}.");

                var (nextEntryId, nextSequence, nextBlock, nextIndexId) =
                    new BlockHeader(BlockBufferSpan.Slice(0, blockHeaderSize), large);
                if (nextEntryId != file) throw new Exception($"Sector data mismatch. Next entry id should be {file}.");

                if (nextSequence != currentSequence)
                    throw new Exception($"Sector data mismatch. Next sequence should be {currentSequence}.");

                if (nextIndexId != type) throw new Exception($"Sector data mismatch. Next index id should be {type}.");

                if (nextBlock < 0) throw new Exception($"Invalid next block position of {nextBlock}.");

                var chunksConsumed = Math.Min(remainingBytes, blockChunkSize);
                Array.Copy(BlockBuffer, blockHeaderSize, data, dataReadIndex, chunksConsumed);
                dataReadIndex += chunksConsumed;
                remainingBytes -= chunksConsumed;
                block = nextBlock;
                currentSequence += 1;
            }

            return data;
        }

        public Index GetIndex(int type)
        {
            return type == 255
                ? _mainDescriptorIndex
                : _indices[type] ?? throw new KeyNotFoundException($"Given index {type} was not found.");
        }

        private readonly ref struct BlockHeader
        {
            public BlockHeader(ReadOnlySpan<byte> data, bool large)
            {
                if (data.Length == 0) throw new Exception("Cannot decode header. Given data cannot be empty.");

                if (data.Length != BlockHeaderSize && data.Length != BlockHeaderExtendedSize)
                    throw new Exception($"Invalid header size of {data.Length} given.");

                if (large)
                {
                    NextEntryId = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) |
                                  data[3];
                    NextSequence = (data[4] << 8) | data[5];
                    NextBlock = (data[6] << 16) | (data[7] << 8) | data[8];
                    NextIndexId = data[9];
                }
                else
                {
                    NextEntryId = (data[0] << 8) | data[1];
                    NextSequence = (data[2] << 8) | data[3];
                    NextBlock = (data[4] << 16) | (data[5] << 8) | data[6];
                    NextIndexId = data[7];
                }
            }

            public void Deconstruct(out int nextEntryId, out int nextSequence, out int nextBlock, out int nextIndexId)
            {
                nextEntryId = NextEntryId;
                nextSequence = NextSequence;
                nextBlock = NextBlock;
                nextIndexId = NextIndexId;
            }

            public override string ToString()
            {
                return
                    $"Header[Next Entry Id: {NextEntryId}, Next Sequence: {NextSequence}, Next Block: {NextBlock}, Next Index Id: {NextIndexId}]";
            }

            public int NextEntryId { get; }
            public int NextSequence { get; }
            public int NextBlock { get; }
            public int NextIndexId { get; }
        }
    }
}