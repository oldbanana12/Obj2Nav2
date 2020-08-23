using System;
using System.Collections.Generic;
using System.IO;

namespace Obj2Nav2.Nav2
{
    public class NavworldNode
    {
        public ScaledVertex Position;
        public Vector3 Vec3Pos;
        public HashSet<NavworldNode> AdjacentNodes;
        public NavworldNode ZeroCostNode;

        public NavworldNode()
        {
            AdjacentNodes = new HashSet<NavworldNode>();
        }

        public ushort Index { get; internal set; }
        public ushort FaceIndex { get; internal set; }

        internal void WritePosition(BinaryWriter writer)
        {
            Position.Write(writer);
        }
    }
}