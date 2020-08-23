using Obj2Nav2.Nav2;
using System;
using System.Collections.Generic;
using System.IO;

namespace Obj2Nav2
{
    internal class SegmentGraphEntry : IEntry
    {
        private const uint PAYLOAD_HEADER_SIZE = 32;

        // FIXME: Hardcoded 1 entry for now 
        private const uint ENTRY_COUNT = 1;

        public byte GroupId;

        public SegmentGraphEntry()
        {

        }

        public EntryType GetEntryType()
        {
            return EntryType.NAVWORLD_SEGMENT_GRAPH;
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
            length += getSubsection3Length();

            return length;
        }

        private uint getSubsection1Length()
        {
            var subsection1Length = (uint)(ENTRY_COUNT * 6);
            subsection1Length += Utils.GetPaddingSize(subsection1Length);
            return subsection1Length;
        }

        private uint getSubsection2Length()
        {
            var subsection2Length = (uint)(ENTRY_COUNT * 12);
            subsection2Length += Utils.GetPaddingSize(subsection2Length);
            return subsection2Length;
        }

        private uint getSubsection3Length()
        {
            return 0;
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

            writer.Write((ushort)32767);
            writer.Write((ushort)0);
            writer.Write((ushort)32767);

            // Padding
            writer.Write((uint)0);
            writer.Write((uint)0);
            writer.Write((ushort)0);

            // Subsection 2
            writer.Write((uint)0);
            writer.Write((ushort)0);
            writer.Write((ushort)65535);

            writer.Write((byte)255);
            writer.Write((byte)255);

            writer.Write((byte)0);
            writer.Write((byte)0);

            // Padding
            writer.Write((uint)0);
        }

        private void writePayloadHeader(BinaryWriter writer)
        {
            writer.Write(PAYLOAD_HEADER_SIZE);
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length()); // Subsection 2 offset
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length() + getSubsection2Length()); // Subsection 3 offset
            writer.Write((uint)0); // Unknown
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length() + getSubsection2Length() + getSubsection3Length()); // Total size

            // TODO: Actually support writing more than 1 entry
            writer.Write((uint)ENTRY_COUNT);

            writer.Write((ushort)0);
            writer.Write((uint)0);
            writer.Write((ushort)0);
        }
    }
}