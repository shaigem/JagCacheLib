using System;
using System.Buffers.Binary;
using System.IO;

namespace JagCacheLib
{
    public class Archive
    {
        
        private const byte HeaderSize = 6;
            
        public Archive(byte[] data)
        {
            var dataSpan = new ReadOnlySpan<byte>(data);

            // TODO decode archive data

        }
    }
}