﻿using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PortableWikiViewer.Core.XZ
{
    public class XZHeader
    {
        public enum CheckType : byte
        {
            NONE = 0x00,
            CRC32 = 0x01,
            CRC64 = 0x04,
            SHA256 = 0x0A
        }

        private readonly BinaryReader _reader;
        private readonly byte[] MagicHeader = { 0xFD, 0x37, 0x7A, 0x58, 0x5a, 0x00 };
        public long StreamStartPosition { get; private set; }

        public CheckType BlockCheckType { get; private set; }
        public int BlockCheckSize => ((((int)BlockCheckType) + 2) / 3) * 4;

        public XZHeader(BinaryReader reader)
        {
            _reader = reader;
            StreamStartPosition = reader.BaseStream.Position;
        }
        public static XZHeader FromStream(Stream stream)
        {
            var header = new XZHeader(new BinaryReader(stream, Encoding.UTF8, true));
            header.Process();
            return header;
        }

        public void Process()
        {
            CheckMagicBytes(_reader.ReadBytes(6));
            ProcessStreamFlags();
        }

        private void ProcessStreamFlags()
        {
            byte[] streamFlags = _reader.ReadBytes(2);
            UInt32 crc = _reader.ReadLittleEndianUInt32();
            UInt32 calcCrc = Crc32.Compute(streamFlags);
            if (crc != calcCrc)
                throw new InvalidDataException("Stream header corrupt");

            BlockCheckType = (CheckType)(streamFlags[1] & 0x0F);
            byte futureUse = (byte)(streamFlags[1] & 0xF0);
            if (futureUse != 0 || streamFlags[0] != 0)
                throw new InvalidDataException("Unknown XZ Stream Version");
        }

        private void CheckMagicBytes(byte[] header)
        {
            if (!Enumerable.SequenceEqual(header, MagicHeader))
                throw new InvalidDataException("Invalid XZ Stream");
        }
    }
}