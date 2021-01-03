using Obj2Nav2.Nav2;
using Obj2Nav2.WavefrontObj;
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

                foreach (var edgeIndex in face.EdgeIndices)
                {
                    writer.Write((byte)(edgeIndex));
                }

                for(int i = 0; i < (7 - face.EdgeIndices.Length); i++)
                {
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

        private class NavworldEdge
        {
            public int index;
            public ScaledVertex A;
            public ScaledVertex B;

            public override bool Equals(object obj)
            {
                return
                    (A.Equals((obj as NavworldEdge).A) && B.Equals((obj as NavworldEdge).B)) ||
                    (A.Equals((obj as NavworldEdge).B) && B.Equals((obj as NavworldEdge).A));
            }
        }

        internal void LoadFromChunks(Obj[,] pieces, uint width_chunks, uint height_chunks)
        {
            var num_chunks = width_chunks * height_chunks;

            ushort vertexOffset = 0;

            for (int y = 0; y < height_chunks; y++)
            {
                for (int x = 0; x < width_chunks; x++)
                {
                    List<NavworldEdge> edges = new List<NavworldEdge>();
                    int newEdgeIndex = 0;

                    ushort vertexCount = 0;

                    var obj = pieces[x, y];
                    foreach (var vertex in obj.VertexList)
                    {
                        var position = new Vector3(vertex.X, vertex.Y, vertex.Z);
                        Vertices.Add(new ScaledVertex(position, Nav2.Nav2.Origin, Nav2.Nav2.ScaleFactor));
                    }

                    foreach (var face in obj.FaceList)
                    {
                        var newFace = new NavmeshFace();
                        newFace.VertexIndices = new int[face.VertexIndexList.Count];
                        newFace.AdjacentFaces = new int[face.VertexIndexList.Count];
                        newFace.EdgeIndices = new int[face.VertexIndexList.Count];
                        newFace.VertexOffset = vertexOffset;

                        for (int i = 0; i < face.VertexIndexList.Count; i++)
                        {
                            newFace.AdjacentFaces[i] = -1;
                            newFace.VertexIndices[i] = face.VertexIndexList[i] - 1;

                            NavworldEdge edge;
                            if (i + 1 >= face.VertexIndexList.Count)
                            {
                                edge = new NavworldEdge() { A = Vertices[vertexOffset + newFace.VertexIndices[i]], B = Vertices[vertexOffset + newFace.VertexIndices[0]] };
                            } else
                            {
                                edge = new NavworldEdge() { A = Vertices[vertexOffset + newFace.VertexIndices[i]], B = Vertices[vertexOffset + face.VertexIndexList[i + 1] - 1] };
                            }

                            bool edgeFound = false;
                            foreach (var existingEdge in edges)
                            {
                                if (edge.Equals(existingEdge))
                                {
                                    newFace.EdgeIndices[i] = existingEdge.index;
                                    edgeFound = true;
                                    break;
                                }
                            }

                            if (!edgeFound)
                            {
                                edge.index = newEdgeIndex;
                                newFace.EdgeIndices[i] = newEdgeIndex;
                                edges.Add(edge);
                                newEdgeIndex++;
                            }

                            vertexCount = (ushort)Math.Max(vertexCount, face.VertexIndexList[i]);
                        }

                        Faces.Add(newFace);
                    }

                    vertexOffset += vertexCount;
                }
            }

            for (int i = 0; i < Faces.Count; i++)
            {
                for (int j = 0; j < Faces.Count; j++)
                {
                    if (i == j)
                        continue;

                    for (int a = 0; a < Faces[i].VertexIndices.Length; a++)
                    {
                        for (int b = 0; b < Faces[j].VertexIndices.Length; b++)
                        {
                            var A1 = Vertices[Faces[i].VertexIndices[a] + Faces[i].VertexOffset];
                            ScaledVertex B1;
                            if (a + 1 >= Faces[i].VertexIndices.Length)
                                B1 = Vertices[Faces[i].VertexIndices[0] + Faces[i].VertexOffset];
                            else
                                B1 = Vertices[Faces[i].VertexIndices[a + 1] + Faces[i].VertexOffset];

                            var A2 = Vertices[Faces[j].VertexIndices[b] + Faces[j].VertexOffset];
                            ScaledVertex B2;
                            if (b + 1 >= Faces[j].VertexIndices.Length)
                                B2 = Vertices[Faces[j].VertexIndices[0] + Faces[j].VertexOffset];
                            else
                                B2 = Vertices[Faces[j].VertexIndices[b + 1] + Faces[j].VertexOffset];

                            if ((A1.Equals(A2) && B1.Equals(B2)) || (A1.Equals(B2) && B1.Equals(A2))) {
                                Faces[i].AdjacentFaces[a] = j;
                                Faces[j].AdjacentFaces[b] = i;
                            }
                        }
                    }
                }
            }
        }
    }
}