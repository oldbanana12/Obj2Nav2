using Obj2Nav2.Nav2;
using System.Collections.Generic;
using System.IO;

namespace Obj2Nav2
{
    internal class SegmentGraphEntry : IEntry
    {
        struct Chunk
        {
            public ScaledVertex Position;
            public HashSet<ushort> AdjacentChunks;
        }

        private const uint PAYLOAD_HEADER_SIZE = 32;

        private ushort num_chunks;
        private uint total_edges;
        private Dictionary<ushort, Chunk> chunk_ids;

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
            var subsection1Length = (uint)(num_chunks * 6);
            subsection1Length += Utils.GetPaddingSize(subsection1Length);
            return subsection1Length;
        }

        private uint getSubsection2Length()
        {
            var subsection2Length = (uint)(num_chunks * 12);
            subsection2Length += Utils.GetPaddingSize(subsection2Length);
            return subsection2Length;
        }

        private uint getSubsection3Length()
        {
            var subsection3Length = (uint)(total_edges * 6);
            subsection3Length += Utils.GetPaddingSize(subsection3Length);
            return subsection3Length;
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

            foreach (var chunk in chunk_ids.Values)
            {
                chunk.Position.Write(writer);
            }

            var padding = Utils.GetPaddingSize((uint)(num_chunks * 6));
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }

            uint section3Offset = 0;
            foreach (var chunk in chunk_ids)
            {
                writer.Write((uint)section3Offset);
                section3Offset += (uint)(chunk.Value.AdjacentChunks.Count * 3);
                writer.BaseStream.Position = writer.BaseStream.Position - 1;

                writer.Write((byte)chunk.Value.AdjacentChunks.Count);
                writer.Write((ushort)0);
                writer.Write((uint)uint.MaxValue);
                writer.Write((byte)0);
                writer.Write((byte)0);
            }

            padding = Utils.GetPaddingSize((uint)(num_chunks * 12));
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }

            foreach (var chunk in chunk_ids.Values)
            {
                foreach (var adjacent in chunk.AdjacentChunks)
                {
                    // TODO: Calculate the actual weight
                    writer.Write((ushort)100);
                    writer.Write((ushort)adjacent);
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                }
            }

            padding = Utils.GetPaddingSize((uint)(total_edges * 6));
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }
        }

        private void writePayloadHeader(BinaryWriter writer)
        {
            writer.Write(PAYLOAD_HEADER_SIZE);
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length()); // Subsection 2 offset
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length() + getSubsection2Length()); // Subsection 3 offset
            writer.Write((uint)0); // Unknown
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length() + getSubsection2Length() + getSubsection3Length()); // Total size

            writer.Write((uint)num_chunks);

            writer.Write((ushort)0);
            writer.Write((uint)total_edges);
            writer.Write((ushort)0);
        }

        internal void LoadFromNavworld(NavworldEntry navworld, uint width_chunks, uint height_chunks)
        {
            chunk_ids = new Dictionary<ushort, Chunk>();

            // TODO: Get the most central point in a chunk
            foreach (var point in navworld.Points)
            {
                if (!chunk_ids.ContainsKey(point.ChunkIndex))
                {
                    var new_chunk = new Chunk
                    {
                        Position = point.Position,
                        AdjacentChunks = new HashSet<ushort>()
                    };

                    chunk_ids.Add(point.ChunkIndex, new_chunk);
                }
            }

            foreach (var chunk in chunk_ids)
            {
                if (chunk_ids.ContainsKey((ushort)(chunk.Key - 1)))
                {
                    chunk.Value.AdjacentChunks.Add((ushort)(chunk.Key - 1));
                    total_edges++;
                }

                if (chunk_ids.ContainsKey((ushort)(chunk.Key + 1)))
                {
                    chunk.Value.AdjacentChunks.Add((ushort)(chunk.Key + 1));
                    total_edges++;
                }

                if (chunk_ids.ContainsKey((ushort)(chunk.Key - width_chunks)))
                {
                    chunk.Value.AdjacentChunks.Add((ushort)(chunk.Key - width_chunks));
                    total_edges++;
                }

                if (chunk_ids.ContainsKey((ushort)(chunk.Key + width_chunks)))
                {
                    chunk.Value.AdjacentChunks.Add((ushort)(chunk.Key + width_chunks));
                    total_edges++;
                }
            }

            num_chunks = (ushort)(chunk_ids.Count);
        }
    }
}