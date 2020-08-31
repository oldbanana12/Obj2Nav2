using Obj2Nav2.Nav2;
using Obj2Nav2.WavefrontObj;
using System;
using System.IO;

namespace Obj2Nav2
{
    internal class SegmentChunkEntry : IEntry
    {
        private const uint PAYLOAD_HEADER_SIZE = 16;

        private uint entry_count;

        public byte GroupId;

        struct Chunk {
            public ScaledVertex from;
            public ScaledVertex to;

            public byte verts;
            public byte faces;
            public byte edges;

            public ushort first_face;

            public ushort max_vert_index;
        }

        private Chunk[] chunks;

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
            var subsection1Length = (uint)(entry_count * 12);
            subsection1Length += Utils.GetPaddingSize(subsection1Length);
            return subsection1Length;
        }

        private uint getSubsection2Length()
        {
            var subsection2Length = (uint)(entry_count * 12);
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

            foreach (var chunk in chunks)
            {
                chunk.from.Write(writer);
                chunk.to.Write(writer);
            }

            var padding = Utils.GetPaddingSize(entry_count * 12);
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }

            ushort max_index = 0;
            foreach (var chunk in chunks)
            {
                writer.Write((ushort)max_index);
                writer.Write((ushort)chunk.first_face);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write((byte)chunk.verts);
                writer.Write((byte)chunk.faces);
                writer.Write((byte)(chunk.faces * 3));
                writer.Write((byte)chunk.edges);

                max_index += chunk.max_vert_index;
            }

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

            writer.Write((uint)entry_count);
        }

        internal void LoadFromChunks(Obj[,] pieces, uint width_chunks, uint height_chunks)
        {
            entry_count = width_chunks * height_chunks;
            chunks = new Chunk[entry_count];

            ushort face_count = 0;
            for (int j = 0; j < height_chunks; j++)
            {
                for (int i = 0; i < width_chunks; i++)
                {
                    var chunk_index = (j * width_chunks) + i;
                    var from = new Vector3(pieces[i, j].Size.XMin, pieces[i, j].Size.YMin, pieces[i, j].Size.ZMin);
                    var to = new Vector3(pieces[i, j].Size.XMax, pieces[i, j].Size.YMax, pieces[i, j].Size.ZMax);

                    foreach (var face in pieces[i, j].FaceList)
                    {
                        foreach (var vertex in face.VertexIndexList)
                        {
                            chunks[chunk_index].max_vert_index = Math.Max((ushort)vertex, chunks[chunk_index].max_vert_index);
                        }
                    }

                    chunks[chunk_index].from = new ScaledVertex(from, Nav2.Nav2.Origin, Nav2.Nav2.ScaleFactor);
                    chunks[chunk_index].to = new ScaledVertex(to, Nav2.Nav2.Origin, Nav2.Nav2.ScaleFactor);
                    chunks[chunk_index].first_face = face_count;
                    face_count += (ushort)pieces[i, j].FaceList.Count;

                    chunks[chunk_index].faces = (byte)pieces[i, j].FaceList.Count;
                    chunks[chunk_index].verts = (byte)pieces[i, j].VertexList.Count;
                    var edges = (pieces[i, j].FaceList.Count * 2) + 1;
                    chunks[chunk_index].edges = (byte)edges;
                }
            }
        }
    }
}