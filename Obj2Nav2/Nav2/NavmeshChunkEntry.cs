using Obj2Nav2.Nav2;
using System;
using System.Collections.Generic;
using System.IO;

namespace Obj2Nav2
{
    internal class NavmeshChunkEntry : IEntry
    {
        private const uint PAYLOAD_HEADER_SIZE = 32;

        public byte GroupId;
        public List<ScaledVertex> Vertices;
        public List<NavmeshFace> Faces;

        public NavmeshChunkEntry()
        {
            Vertices = new List<ScaledVertex>();
            Faces = new List<NavmeshFace>();
        }

        public EntryType GetEntryType()
        {
            return EntryType.NAVMESH_CHUNK;
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
            var subsection1Length = (uint)(Vertices.Count * 6);
            subsection1Length += Utils.GetPaddingSize(subsection1Length);
            return subsection1Length;
        }

        private uint getSubsection2Length()
        {
            var subsection2Length = (uint)(Faces.Count * 4);
            subsection2Length += Utils.GetPaddingSize(subsection2Length);
            return subsection2Length;
        }
        private uint getSubsection3Length()
        {
            var subsection3Length = (uint)(Faces.Count * 16);
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

            foreach(var vertex in Vertices)
            {
                vertex.Write(writer);
            }

            var padding = Utils.GetPaddingSize((uint)(Vertices.Count * 6));
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }

            uint faceOffset = 0;
            foreach(var face in Faces)
            {
                // TODO: Handle the cases of 4-vertex faces and off-mesh adjacent edges
                writer.Write(faceOffset);
                faceOffset += 8;
            }

            padding = Utils.GetPaddingSize((uint)(Faces.Count * 4));
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }

            var faceIndex = 0;
            foreach(var face in Faces)
            {
                foreach (var adjacent in face.AdjacentFaces)
                {
                    writer.Write((ushort)adjacent);
                }

                foreach (var vertexIndex in face.VertexIndices)
                {
                    writer.Write((byte)(vertexIndex));
                }

                for(int i = 0; i < 7; i++)
                {
                    // TODO: There's 3-4 values here that actually mean something and should have a value
                    writer.Write((byte)0);
                }

                faceIndex++;
            }
        }

        private void writePayloadHeader(BinaryWriter writer)
        {
            writer.Write(PAYLOAD_HEADER_SIZE);
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length()); // Subsection 2 offset
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length() + getSubsection2Length()); // Subsection 3 offset

            writer.Write((uint)0);

            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);

            writer.Write((ushort)Faces.Count);
            writer.Write((ushort)Vertices.Count);

            writer.Write((ushort)0);
            writer.Write((ushort)0);
        }
    }
}