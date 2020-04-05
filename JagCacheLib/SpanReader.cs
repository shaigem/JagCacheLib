using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace JagCacheLib
{
    public ref struct SpanReader
    {
        private readonly byte[] _backingData;
        private readonly ReadOnlySpan<byte> BackingDataSpan => _backingData;

        public int Offset { get; private set; }

        public int Remaining => _backingData.Length - Offset;

        public SpanReader(byte[] data, int offset = 0)
        {
            _backingData = data;
            Offset = offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32BigEndian()
        {
            const int size = sizeof(int);
            var result = BinaryPrimitives.ReadInt32BigEndian(BackingDataSpan.Slice(Offset, size));
            Offset += size;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt16BigEndian()
        {
            const int size = sizeof(short);
            var result = BinaryPrimitives.ReadInt16BigEndian(BackingDataSpan.Slice(Offset, size));
            Offset += size;
            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadUInt16BigEndian()
        {
            const int size = sizeof(ushort);
            var result = BinaryPrimitives.ReadUInt16BigEndian(BackingDataSpan.Slice(Offset, size));
            Offset += size;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadUIntSmartBigEndian()
        {
            var peek = PeekByte();
            if ((peek & 0x80) == 0) return ReadInt16BigEndian() & 0x7fff;

            return ReadInt32BigEndian() & 0x7fffffff;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(byte[] array, int offset, int count)
        {
            var destination = new Span<byte>(array, offset, count);
            var bytesRead = Math.Min(Remaining, destination.Length);
            new Span<byte>(_backingData, Offset, bytesRead).CopyTo(destination);
            Offset += bytesRead;
            return bytesRead;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ReadSlice(int count)
        {
            var bytesRead = Math.Min(Remaining, count);
            var result = BackingDataSpan.Slice(Offset, bytesRead);
            Offset += bytesRead;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            const int size = sizeof(byte);
            var result = _backingData[Offset];
            Offset += size;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte PeekByte()
        {
            // TODO offset validation
            return _backingData[Offset];
        }
    }
}