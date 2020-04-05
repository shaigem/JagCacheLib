using System;

namespace JagCacheLib
{
    public class ReferenceTable
    {
        private const byte FormatVersion = 6;
        private const byte FormatVersionSmart = 7;

        private ReferenceTable()
        {
        }

        public int Format { get; set; }

        public static ReferenceTable FromBytes(byte[] data)
        {
            var reader = new SpanReader(data);

            var table = new ReferenceTable();
            var format = reader.ReadByte();

            if (format < FormatVersion || format > FormatVersionSmart)
                throw new ArgumentException($"Unknown reference table format of {format} given.");

            var version = format >= FormatVersion ? reader.ReadInt32BigEndian() : 0;
            var flags = reader.ReadByte();
            var entryCount = format >= FormatVersionSmart
                ? reader.ReadUIntSmartBigEndian()
                : reader.ReadInt16BigEndian();
            Console.WriteLine($"{format} {version}, {flags}, {entryCount}");


            return table;
        }
    }
}