using System;

namespace JagCacheLib
{
    public class Archive
    {
        public Archive(int id)
        {
            Id = id;
        }

        public int Id { get; }

        public int NameHash { get; set; }

        public int Crc { get; set; }

        public int Version { get; set; }

        public ArchiveFile[] Files { get; set; } = Array.Empty<ArchiveFile>();

        public byte[] WhirlpoolDigest { get; set; } = Array.Empty<byte>();

        public override string ToString()
        {
            return
                $"{nameof(Id)}: {Id}, {nameof(NameHash)}: {NameHash}, {nameof(Crc)}: {Crc}, {nameof(Version)}: {Version}";
        }
    }
}