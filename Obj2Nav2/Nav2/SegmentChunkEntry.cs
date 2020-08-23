using Obj2Nav2.Nav2;
using System;
using System.Collections.Generic;
using System.IO;

namespace Obj2Nav2
{
    internal class SegmentChunkEntry : IEntry
    {
        private const uint PAYLOAD_HEADER_SIZE = 16;

        // FIXME: Hardcoded 1 entry for now 
        private const uint ENTRY_COUNT = 1;

        public byte GroupId;

        public byte verts;
        public byte faces;
        public byte edges;

        public SegmentChunkEntry()
        {

        }

        public EntryType GetEntryType()
        {
            return EntryType.SEGMENT_CHUNK;
        }

        public byte GetGroupId()
        {
            return GroupId;
        }

        public uint GetLength()
        {
            return getTotalLength();
        }

        private uint getTotalLength()
        {
            return getDataLength() + Utils.GetPaddingSize(getDataLength());
        }

        private uint getDataLength()
        {
            uint length = 0;
            length += 16; // Entry header
            length += PAYLOAD_HEADER_SIZE; // Payload header
            length += getSubsection1Length();
            length += getSubsection2Length();

            return length;
        }

        private uint getSubsection1Length()
        {
            var subsection1Length = (uint)(ENTRY_COUNT * 12);
            subsection1Length += Utils.GetPaddingSize(subsection1Length);
            return subsection1Length;
        }

        private uint getSubsection2Length()
        {
            var subsection2Length = (uint)(ENTRY_COUNT * 12);
            subsection2Length += Utils.GetPaddingSize(subsection2Length);
            return subsection2Length;
        }

        public void Write(BinaryWriter writer)
        {
            writeHeader(writer);
            writePayload(writer);
        }

        private void writeHeader(BinaryWriter writer)
        {
            writer.Write((ushort)GetEntryType());
            writer.Write((ushort)0); // Unknown

            writer.Write(getTotalLength()); // Total entry length
            writer.Write((uint)16); // Header length

            writer.Write(GroupId);

            writer.Write((byte)0); // Unknown
            writer.Write((ushort)0); // Unknown
        }

        private void writePayload(BinaryWriter writer)
        {
            writePayloadHeader(writer);

            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);

            writer.Write((ushort)ushort.MaxValue);
            writer.Write((ushort)ushort.MaxValue);
            writer.Write((ushort)ushort.MaxValue);

            // Padding
            writer.Write((ushort)0);
            writer.Write((ushort)0);

            // Subsection 2
            writer.Write((ushort)0);
            writer.Write((ushort)0); 
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((byte)verts);
            writer.Write((byte)faces);
            writer.Write((byte)(faces * 3));
            writer.Write((byte)edges);

            // Padding
            writer.Write((ushort)0);
            writer.Write((ushort)0);
        }

        private void writePayloadHeader(BinaryWriter writer)
        {
            writer.Write(PAYLOAD_HEADER_SIZE);
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length()); // Subsection 2 offset
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length() + getSubsection2Length()); // Subsection 3 offset

            // TODO: Actually support writing more than 1 entry
            writer.Write((uint)ENTRY_COUNT);
        }
    }
}