using System;
using System.IO;
using Ionic.BZip2;

namespace JagCacheLib
{
    public enum CompressionType
    {
        None = 0,
        Bzip2 = 1
    }

    public static class CompressionHelper
    {
        private static ReadOnlySpan<byte> Bzip2Header => new[] {(byte) 'B', (byte) 'Z', (byte) 'h', (byte) '1'};

        public static byte[] DecompressBzip2(ReadOnlySpan<byte> data, int uncompressedLength)
        {
            var dataToDecompress = new Span<byte>(new byte[data.Length + Bzip2Header.Length]);

            Bzip2Header.CopyTo(dataToDecompress);
            data.CopyTo(dataToDecompress.Slice(Bzip2Header.Length));

            using var inputStream = new MemoryStream(data.Length);
            inputStream.Write(dataToDecompress);
            inputStream.Position = 0;

            //| SpanReader | 10 | 137.4 ms | 0.42 ms | 0.37 ms |    1 | 3750.0000 | 3250.0000 | 3250.0000 |  13.08 MB |
            //| SpanReader | 10 | 131.8 ms | 0.28 ms | 0.25 ms |    1 | 3500.0000 | 3000.0000 | 3000.0000 |  13.04 MB |

            using var bzipInputStream = new BZip2InputStream(inputStream);
            var uncompressedData = new byte[uncompressedLength];
            bzipInputStream.Read(uncompressedData);

            return uncompressedData;
        }
    }


    public readonly ref struct Container
    {
        public byte[] Data { get; }


        public Container(byte[] data)
        {
            var reader = new SpanReader(data);
            var compressionType = (CompressionType) reader.ReadByte();
            var length = reader.ReadInt32BigEndian();

            // TODO check if valid compression type

            // TODO xtea decrypt

            var uncompressedLength = compressionType == CompressionType.None ? length : reader.ReadInt32BigEndian();

            var dataSlice = reader.ReadSlice(length);

            dataSlice = compressionType switch
            {
                CompressionType.None => dataSlice,
                CompressionType.Bzip2 => CompressionHelper.DecompressBzip2(dataSlice, uncompressedLength),
                _ => throw new ArgumentException($"Invalid compression type of {compressionType}.")
            };

            Data = dataSlice.ToArray();
        }
    }
}