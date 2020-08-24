using Obj2Nav2.WavefrontObj;
using System;
using System.Collections.Generic;
using System.IO;

namespace Obj2Nav2.Nav2
{
    class NavworldEntry : IEntry
    {
        class GeometryEdge : IEquatable<GeometryEdge>
        {
            public Vertex A;
            public Vertex B;

            public ushort EdgeIndex { get; internal set; }

            public bool Equals(GeometryEdge other)
            {
                return (other.A == A && other.B == B) || (other.B == A && other.A == B);
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as GeometryEdge);
            }

            public override int GetHashCode()
            {
                int result = 37;

                result *= 397;
                result += GetMidpoint().GetHashCode();

                return result;
            }

            public Vertex GetMidpoint()
            {
                return new Vertex((A.X + B.X) / 2, (A.Y + B.Y) / 2, (A.Z + B.Z) / 2);
            }
        }

        class Face
        {
            public ushort Index;
            public int[] VertexIndexList;
            public GeometryEdge[] Edges;
            public Face[] AdjacentFaces;
            public Midpoint[] Midpoints;
        }

        public class Midpoint : IEquatable<Midpoint>
        {
            public ushort Index;
            public Vertex Position;

            public override bool Equals(object obj)
            {
                return this.Equals(obj as Midpoint);
            }

            public bool Equals(Midpoint other)
            {
                return Position.Equals(other.Position);
            }
        }

        private const uint PAYLOAD_HEADER_SIZE = 48;
        private const uint WEIGHT_SCALE = 10;

        public byte GroupId;

        public List<NavworldNode> Points;
        private List<NavworldEdge> edges;

        private uint num_chunks;
        private ushort[] edge_offsets;

        public NavworldEntry()
        {
            Points = new List<NavworldNode>();
            edges = new List<NavworldEdge>();
        }

        public uint GetLength()
        {
            computeEdges();
            return getTotalLength();
        }

        public byte GetGroupId()
        {
            return GroupId;
        }

        public EntryType GetEntryType()
        {
            return EntryType.NAVWORLD;
        }

        public void Write(BinaryWriter writer)
        {
            computeEdges();

            writeHeader(writer);
            writePayload(writer);
        }

        private void computeEdges()
        {
            edges.Clear();

            ushort nextEdgeIndex = 0;
            foreach (var point in Points)
            {
                foreach (var adjacent in point.AdjacentNodes)
                {
                    var newEdge = new NavworldEdge();
                    newEdge.A = point;
                    newEdge.B = adjacent;
                    newEdge.ChunkIndex = point.ChunkIndex;

                    double squared =
                       Math.Pow(((point.Vec3Pos.X * WEIGHT_SCALE) - (adjacent.Vec3Pos.X * WEIGHT_SCALE)), 2) +
                       Math.Pow(((point.Vec3Pos.Y * WEIGHT_SCALE) - (adjacent.Vec3Pos.Y * WEIGHT_SCALE)), 2) +
                       Math.Pow(((point.Vec3Pos.Z * WEIGHT_SCALE) - (adjacent.Vec3Pos.Z * WEIGHT_SCALE)), 2);

                    double distance = Math.Sqrt(squared);
                    newEdge.Weight = (ushort)distance;

                    bool edgeFound = false;

                    foreach (var edge in edges)
                    {
                        if (newEdge.Equals(edge))
                        {
                            edgeFound = true;
                            break;
                        }
                    }

                    if (!edgeFound)
                    {
                        newEdge.Index = nextEdgeIndex;
                        edges.Add(newEdge);
                        nextEdgeIndex++;
                    }
                }
            }
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

        private void writePayloadHeader(BinaryWriter writer)
        {
            writer.Write(PAYLOAD_HEADER_SIZE);
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length()); // Subsection 2 offset
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length() + getSubsection2Length() + getSubsection3Length()); // Subsection 4 offset
            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length() + getSubsection2Length()); // Subsection 3 offset

            writer.Write((uint)0);
            writer.Write((uint)0);

            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length() + getSubsection2Length() + getSubsection3Length() + getSubsection4Length()); // Subsection 5 offset

            writer.Write((uint)0);

            writer.Write(PAYLOAD_HEADER_SIZE + getSubsection1Length() + getSubsection2Length() + getSubsection3Length() + getSubsection4Length() + getSubsection5Length()); // Subsection 6 offset

            writer.Write((ushort)Points.Count);
            writer.Write((ushort)edges.Count);

            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)0);

            writer.Write((ushort)num_chunks);
        }

        internal void LoadFromChunks(Obj[,] pieces, uint width_chunks, uint height_chunks)
        {
            num_chunks = width_chunks * height_chunks;
            edge_offsets = new ushort[num_chunks];

            ushort faceIndex = 0;
            ushort edgeIndex = 0;
            ushort chunkIndex = 0;
            for (int y = 0; y < height_chunks; y++)
            {
                for(int x = 0; x < width_chunks; x++)
                {
                    edge_offsets[chunkIndex] = edgeIndex;

                    var obj = pieces[x, y];
                    var faces = new List<Face>();
                    var edges = new HashSet<GeometryEdge>();

                    foreach (var objFace in obj.FaceList)
                    {
                        var face = new Face
                        {
                            Index = faceIndex,
                            VertexIndexList = new int[objFace.VertexIndexList.Count],
                            Edges = new GeometryEdge[objFace.VertexIndexList.Count],
                            AdjacentFaces = new Face[objFace.VertexIndexList.Count],
                            Midpoints = new Midpoint[objFace.VertexIndexList.Count]
                        };

                        for (int i = 0; i < objFace.VertexIndexList.Count; i++)
                        {
                            face.VertexIndexList[i] = objFace.VertexIndexList[i] - 1;
                            var a = obj.VertexList[objFace.VertexIndexList[i] - 1];
                            Vertex b;
                            if (i + 1 >= objFace.VertexIndexList.Count)
                            {
                                b = obj.VertexList[objFace.VertexIndexList[0] - 1];
                            }
                            else
                            {
                                b = obj.VertexList[objFace.VertexIndexList[i + 1] - 1];
                            }


                            var edge = new GeometryEdge { A = a, B = b, EdgeIndex = edgeIndex };
                            if (!edges.Contains (edge))
                            {
                                edges.Add(edge);
                                edgeIndex++;
                            }

                            edges.TryGetValue(edge, out edge);
                            face.Edges[i] = edge;
                            face.Midpoints[i] = new Midpoint
                            {
                                Index = edge.EdgeIndex,
                                Position = edge.GetMidpoint()
                            };

                            foreach (var otherFace in faces)
                            {
                                if (face == otherFace)
                                {
                                    continue;
                                }

                                for (int j = 0; j < otherFace.Edges.Length; j++)
                                {
                                    if (face.Edges[i].Equals(otherFace.Edges[j]))
                                    {
                                        face.AdjacentFaces[i] = otherFace;
                                        otherFace.AdjacentFaces[j] = face;
                                    }
                                }
                            }
                        }

                        faceIndex++;

                        faces.Add(face);
                    }

                    foreach (var face in faces)
                    {
                        for (int i = 0; i < face.Midpoints.Length; i++)
                        {
                            bool found = false;
                            foreach (var point in Points)
                            {
                                if(point.Index == face.Midpoints[i].Index)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                                continue;

                            var position = new Vector3(face.Midpoints[i].Position.X, face.Midpoints[i].Position.Y, face.Midpoints[i].Position.Z);

                            var node = new NavworldNode
                            {
                                Position = new ScaledVertex(position, Nav2.Origin, Nav2.ScaleFactor),
                                Vec3Pos = position,
                                Index = face.Midpoints[i].Index,
                                FaceIndex = face.Index,
                                ChunkIndex = chunkIndex
                            };

                            Points.Add(node);
                        }
                    }

                    foreach (var face in faces)
                    {
                        for (int i = 0; i < face.Midpoints.Length; i++)
                        {
                            for (int j = 0; j < face.Midpoints.Length; j++)
                            {
                                if (i == j)
                                    continue;

                                Points[face.Midpoints[i].Index].AdjacentNodes.Add(Points[face.Midpoints[j].Index]);
                                Points[face.Midpoints[j].Index].AdjacentNodes.Add(Points[face.Midpoints[i].Index]);
                            }
                        }
                    }

                    chunkIndex++;
                }
            }

            for (int i = 0; i < Points.Count; i++)
            {
                for (int j = 0; j < Points.Count; j++)
                {
                    if (i == j)
                        continue;

                    if (Points[i].Position.Equals(Points[j].Position))
                    {
                        Points[i].ZeroCostNode = Points[j];
                        Points[j].ZeroCostNode = Points[i];
                    }
                }
            }
        }

        private void writePayload(BinaryWriter writer)
        {
            writePayloadHeader(writer);

            foreach (var point in Points)
            {
                point.WritePosition(writer);
            }

            var padding = Utils.GetPaddingSize((uint)(Points.Count * 6));
            for (int i = 0; i < padding; i++) {
                writer.Write((byte)0);
            }

            ushort nextSectionOffset = 0;
            foreach (var point in Points)
            {
                writer.Write(nextSectionOffset);
                writer.Write((ushort)point.ChunkIndex);

                writer.Write((byte)(point.AdjacentNodes.Count));
                writer.Write((byte)(point.ZeroCostNode == null ? 0 : 1));

                nextSectionOffset += (ushort)((point.AdjacentNodes.Count * 2) + (point.ZeroCostNode == null ? 0 : 1));
            }

            padding = Utils.GetPaddingSize((uint)(Points.Count * 6));
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }

            foreach (var point in Points)
            {
                foreach (var adjacent in point.AdjacentNodes)
                {
                    writer.Write(adjacent.Index);

                    var newEdge = new NavworldEdge();
                    newEdge.A = point;
                    newEdge.B = adjacent;

                    foreach (var edge in edges)
                    {
                        if (newEdge.Equals(edge))
                        {
                            writer.Write(edge.Index);
                            break;
                        }
                    }
                }

                if (point.ZeroCostNode != null)
                {
                    writer.Write(point.ZeroCostNode.Index);
                }
            }

            padding = Utils.GetPaddingSize(getSubsection3LengthWithoutPadding());
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }

            foreach (var edge in edges)
            {
                writer.Write((ushort)edge.Weight); // weight
                writer.Write((ushort)edge.ChunkIndex); // subsection5 index
                writer.Write((byte)(edge.A.Index - edge_offsets[edge.A.ChunkIndex]));
                writer.Write((byte)(edge.B.Index - edge_offsets[edge.B.ChunkIndex]));
            }

            padding = Utils.GetPaddingSize((uint)(edges.Count * 6));
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }

            for (int i = 0; i < num_chunks; i++)
            {
                // TODO: Find what different values mean, flags?
                writer.Write((ushort)1249);
            }

            padding = Utils.GetPaddingSize(2 * num_chunks);
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }

            foreach (var point in Points)
            {
                writer.Write(point.FaceIndex);
            }

            padding = Utils.GetPaddingSize((uint)(Points.Count * 2));
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }

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
            length += getSubsection4Length();
            length += getSubsection5Length();
            length += getSubsection6Length();

            return length;
        }

        private uint getSubsection1Length()
        {
            var subsection1Length = (uint)(Points.Count * 6);
            subsection1Length += Utils.GetPaddingSize(subsection1Length);

            return subsection1Length;
        }

        private uint getSubsection2Length()
        {
            var subsection2Length = (uint)(Points.Count * 6);
            subsection2Length += Utils.GetPaddingSize(subsection2Length);

            return subsection2Length;
        }
        private uint getSubsection3LengthWithoutPadding()
        {
            uint count = 0;
            foreach (var point in Points)
            {
                count += (uint)(point.AdjacentNodes.Count * 4);
                count += point.ZeroCostNode == null ? (uint)0 : (uint)2;
            }

            return count;
        }
        private uint getSubsection3Length()
        {
            var subsection3Length = getSubsection3LengthWithoutPadding();
            subsection3Length += Utils.GetPaddingSize(subsection3Length);
            return subsection3Length;
        }
        private uint getSubsection4Length()
        {
            var subsection4Length = (uint)(edges.Count * 6);
            subsection4Length += Utils.GetPaddingSize(subsection4Length);

            return subsection4Length;
        }
        private uint getSubsection5Length()
        {
            var subsection5Length = (uint)(2 * num_chunks);
            subsection5Length += Utils.GetPaddingSize(subsection5Length);
            return subsection5Length;
        }
        private uint getSubsection6Length()
        {
            var subsection6Length = (uint)(Points.Count * 2);
            subsection6Length += Utils.GetPaddingSize(subsection6Length);
            return subsection6Length;
        }
    }
}
