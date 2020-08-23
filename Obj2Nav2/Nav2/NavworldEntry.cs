using System;
using System.Collections.Generic;
using System.IO;

namespace Obj2Nav2.Nav2
{
    class NavworldEntry : IEntry
    {
        private const uint PAYLOAD_HEADER_SIZE = 48;
        private const uint WEIGHT_SCALE = 10;

        public byte GroupId;

        public List<NavworldNode> Points;
        private List<NavworldEdge> edges;

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

            // TODO: Num subsection 5 entries
            writer.Write((ushort)1);
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
                writer.Write((ushort)0);

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
                writer.Write((ushort)0); // subsection5 index
                writer.Write((byte)edge.A.Index);
                writer.Write((byte)edge.B.Index);
            }

            padding = Utils.GetPaddingSize((uint)(edges.Count * 6));
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }

            // TODO: Allow multiple subsection5 entries
            writer.Write((ushort)1249);
            padding = Utils.GetPaddingSize(2);
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }

            foreach (var edge in edges)
            {
                writer.Write(edge.A.FaceIndex);
            }

            padding = Utils.GetPaddingSize((uint)(edges.Count * 2));
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
            var subsection5Length = (uint)2;
            subsection5Length += Utils.GetPaddingSize(subsection5Length);
            return subsection5Length;
        }
        private uint getSubsection6Length()
        {
            var subsection6Length = (uint)(edges.Count * 2);
            subsection6Length += Utils.GetPaddingSize(subsection6Length);
            return subsection6Length;
        }
    }
}
