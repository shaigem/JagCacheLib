using System;
using System.IO;
using System.IO.Compression;
using Ionic.BZip2;

namespace JagCacheLib
{
    public enum CompressionType
    {
        None = 0,
        Bzip2 = 1,
        Gzip = 2
    }

    public static class CompressionHelper
    {
        private const byte Bzip2HeaderLength = 4;
        private static ReadOnlySpan<byte> Bzip2Header => new[] {(byte) 'B', (byte) 'Z', (byte) 'h', (byte) '1'};

        public static byte[] DecompressBzip2(ReadOnlySpan<byte> compressedDataNoHeader, int uncompressedLength)
        {
            var compressedDataLengthNoHeader = compressedDataNoHeader.Length;
            var compressedDataWithHeader = new byte[compressedDataLengthNoHeader + Bzip2HeaderLength];
            var compressedDataWithHeaderSpan = compressedDataWithHeader.AsSpan();
            Bzip2Header.CopyTo(compressedDataWithHeaderSpan);
            compressedDataNoHeader.CopyTo(compressedDataWithHeaderSpan.Slice(Bzip2HeaderLength));
            using var inputStream = new MemoryStream(compressedDataLengthNoHeader);
            inputStream.Write(compressedDataWithHeaderSpan);
            inputStream.Position = 0;
            using var bzipInputStream = new BZip2InputStream(inputStream);
            var uncompressedData = new byte[uncompressedLength];
            bzipInputStream.Read(uncompressedData);
            return uncompressedData;
        }

        public static byte[] DecompressGzip(ReadOnlySpan<byte> compressedDataNoHeader, int uncompressedLength)
        {
            var compressedDataLengthNoHeader = compressedDataNoHeader.Length;
            using var inputStream = new MemoryStream(compressedDataLengthNoHeader);
            inputStream.Write(compressedDataNoHeader);
            inputStream.Position = 0;
            using var gzipInputStream = new GZipStream(inputStream, CompressionMode.Decompress);
            var uncompressedData = new byte[uncompressedLength];
            gzipInputStream.Read(uncompressedData);
            return uncompressedData;
        }
    }

    public class Container
    {
        public Container(byte[] data)
        {
            var reader = new SpanReader(data);
            var compressionType = (CompressionType) reader.ReadByte();
            var length = reader.ReadInt32BigEndian();

            // TODO check if valid compression type

            // TODO xtea decrypt

            var uncompressedLength = compressionType == CompressionType.None ? length : reader.ReadInt32BigEndian();

            Data = compressionType switch
            {
                CompressionType.None => reader.ReadSlice(length).ToArray(),
                CompressionType.Bzip2 =>
                CompressionHelper.DecompressBzip2(reader.ReadSlice(length), uncompressedLength),
                CompressionType.Gzip => CompressionHelper.DecompressGzip(reader.ReadSlice(length), uncompressedLength),
                _ => throw new ArgumentException($"Invalid compression type of {compressionType}.")
            };
        }

        public byte[] Data { get; }
    }
}