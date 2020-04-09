using System;

namespace JagCacheLib
{
    public class ReferenceTable
    {
        private const byte FormatVersion = 6;
        private const byte FormatVersionSmart = 7;
        private const byte FlagNameHashes = 0x01;
        private const byte FlagWhirlpool = 0x02;
        private const byte FlagUnknown4 = 0x04;
        private const byte FlagUnknown8 = 0x08;
        private const byte WhirlpoolDigestLength = 64;

        private ReferenceTable(int format, int version, Archive[] archives)
        {
            Format = format;
            Version = version;
            Archives = archives;
        }

        public int Format { get; }

        public int Version { get; }

        public Archive[] Archives { get; }

        public static ReferenceTable Decode(byte[] data)
        {
            var reader = new SpanReader(data);
            var format = reader.ReadByte();

            if (format < FormatVersion || format > FormatVersionSmart)
                throw new ArgumentException($"Unknown reference table format of {format} given.");

            var version = format >= FormatVersion ? reader.ReadInt32BigEndian() : 0;
            var flags = reader.ReadByte();
            var hasNames = (flags & FlagNameHashes) != 0;
            var hasWhirlpool = (flags & FlagWhirlpool) != 0;
            var hasUnknown4 = (flags & FlagUnknown4) != 0;
            var hasUnknown8 = (flags & FlagUnknown8) != 0;

            var entryCount = format >= FormatVersionSmart
                ? reader.ReadUIntSmartBigEndian()
                : reader.ReadUInt16BigEndian();


            var entries = new Archive[entryCount];

            // create the Archive objects
            var lastEntryId = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                var id = lastEntryId += format >= FormatVersionSmart
                    ? reader.ReadUIntSmartBigEndian()
                    : reader.ReadUInt16BigEndian();
                entries[i] = new Archive(id);
            }

            // if the reference table has identifiers, read them
            if (hasNames)
            {
                for (var i = 0; i < entryCount; i++)
                {
                    entries[i].NameHash = reader.ReadInt32BigEndian();
                }
            }

            // read the checksums
            for (var i = 0; i < entryCount; i++)
            {
                entries[i].Crc = reader.ReadInt32BigEndian();
            }

            // TODO identify this
            if (hasUnknown8)
            {
                for (var i = 0; i < entryCount; i++)
                {
                    reader.ReadInt32BigEndian();
                }
            }

            // read the whirlpool digests
            if (hasWhirlpool)
            {
                for (var i = 0; i < entryCount; i++)
                {
                    var whirlpoolSlice = reader.ReadSlice(WhirlpoolDigestLength);
                    entries[i].WhirlpoolDigest = whirlpoolSlice.ToArray();
                }
            }

            // TODO identify this
            if (hasUnknown4)
            {
                for (var i = 0; i < entryCount; i++)
                {
                    reader.ReadInt32BigEndian();
                    reader.ReadInt32BigEndian();
                }
            }

            // read the entry versions
            for (var i = 0; i < entryCount; i++)
            {
                entries[i].Version = reader.ReadInt32BigEndian();
            }

            // create our child entries
            for (var i = 0; i < entryCount; i++)
            {
                var count = format >= FormatVersionSmart
                    ? reader.ReadUIntSmartBigEndian()
                    : reader.ReadUInt16BigEndian();

                var entry = entries[i];
                entry.Files = new ArchiveFile[count];
            }

            // read the child entry ids
            for (var i = 0; i < entryCount; i++)
            {
                var childEntries = entries[i].Files;
                var childEntriesLength = childEntries.Length;
                var lastChildEntryId = 0;
                for (var j = 0; j < childEntriesLength; j++)
                {
                    var id = lastChildEntryId += format >= FormatVersionSmart
                        ? reader.ReadUIntSmartBigEndian()
                        : reader.ReadUInt16BigEndian();

                    var childEntry = childEntries[j];

                    childEntry.Id = id;
                }
            }

            // if the child entries have identifiers, read them
            if (hasNames)
            {
                for (var i = 0; i < entryCount; i++)
                {
                    var childEntries = entries[i].Files;
                    var childEntriesLength = childEntries.Length;
                    for (var j = 0; j < childEntriesLength; j++)
                    {
                        var identifier = reader.ReadInt32BigEndian();
                        var childEntry = childEntries[j];
                        childEntry.NameHash = identifier;
                    }
                }
            }

            return new ReferenceTable(format, version, entries);
        }
    }
}