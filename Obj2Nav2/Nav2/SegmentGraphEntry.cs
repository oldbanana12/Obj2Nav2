using Obj2Nav2.Nav2;
using System.Collections.Generic;
using System.IO;

namespace Obj2Nav2
{
    internal class SegmentGraphEntry : IEntry
    {
        struct Chunk
        {
            public ScaledVertex Center;
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

        public ushort[] GetChunkIDs ()
        {
            ushort[] ids = new ushort[chunk_ids.Keys.Count];
            chunk_ids.Keys.CopyTo(ids, 0);
            return ids;
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
                    var distance = (chunk.Position - chunk_ids[adjacent].Position).Magnitude () / 100;
                    writer.Write((ushort)distance);
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

            foreach (var point in navworld.Points)
            {
                if (!chunk_ids.ContainsKey(point.ChunkIndex))
                {
                    var new_chunk = new Chunk
                    {
                        Center = new ScaledVertex(
                            new Vector3(
                                navworld.ChunkSizes[point.ChunkIndex].XMax - (navworld.ChunkSizes[point.ChunkIndex].XSize / 2),
                                navworld.ChunkSizes[point.ChunkIndex].YMax - (navworld.ChunkSizes[point.ChunkIndex].YSize / 2),
                                navworld.ChunkSizes[point.ChunkIndex].ZMax - (navworld.ChunkSizes[point.ChunkIndex].ZSize / 2)
                            ),
                            Nav2.Nav2.Origin,
                            Nav2.Nav2.ScaleFactor
                        ),
                        Position = point.Position,
                        AdjacentChunks = new HashSet<ushort>()
                    };

                    chunk_ids.Add(point.ChunkIndex, new_chunk);
                } 
                else
                {
                    var orig_dist = (chunk_ids[point.ChunkIndex].Center - chunk_ids[point.ChunkIndex].Position).Magnitude();
                    var new_dist = (point.Position - chunk_ids[point.ChunkIndex].Center).Magnitude();

                    var orig_center = chunk_ids[point.ChunkIndex].Center;
                    if (new_dist < orig_dist)
                    {
                        chunk_ids[point.ChunkIndex] = new Chunk
                        {
                            Center = orig_center,
                            Position = point.Position,
                            AdjacentChunks = new HashSet<ushort>()
                        };
                    }
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