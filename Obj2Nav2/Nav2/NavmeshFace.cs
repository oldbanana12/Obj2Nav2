﻿namespace Obj2Nav2
{
    public class NavmeshFace
    {
        public int[] AdjacentFaces;
        public int[] VertexIndices;

        public ushort VertexOffset;

        public int[] EdgeIndices { get; internal set; }
    }
}