﻿using System;
using System.IO;

namespace Obj2Nav2.Nav2
{
    class NavSystem
    {
        public Vector3 Origin;
        public Vector3 Size;

        private const uint HEADER_SIZE = 48;
        private const uint ENTRY_COUNT = 1;
        internal uint GetLength()
        {
            return HEADER_SIZE;
        }

        internal void Write(BinaryWriter writer)
        {
            WriteHeader(writer);

            writer.Write((uint)16);
            writer.Write((ushort)1);
            writer.Write((byte)0);
            writer.Write((byte)255);

            // Padding
            writer.Write((uint)0);
            writer.Write((uint)0);

            // Subsection 2
            writer.Write((byte)4);
            writer.Write((byte)0);
            writer.Write((ushort)0);
            writer.Write((uint)1);
            writer.Write((ushort)0);

        }

        private void WriteHeader(BinaryWriter writer)
        {
            writer.Write((float)Origin.X);
            writer.Write((float)Origin.Y);
            writer.Write((float)Origin.Z);
            writer.Write((float)0);

            writer.Write((uint)1);
            writer.Write((uint)1);
            writer.Write((uint)1);

            writer.Write((uint)10);
            writer.Write((uint)(Size.X * 10));

            writer.Write(HEADER_SIZE);
            writer.Write(HEADER_SIZE + getSubsection1Length()); // Subsection 2 offset
            writer.Write(HEADER_SIZE + getSubsection1Length() + getSubsection2Length()); // Subsection 3 offset
        }

        private uint getSubsection1Length()
        {
            var subsection1Length = (uint)(ENTRY_COUNT * 8);
            subsection1Length += Utils.GetPaddingSize(subsection1Length);
            return subsection1Length;
        }

        private uint getSubsection2Length()
        {
            var subsection2Length = (uint)(ENTRY_COUNT * 10);
            subsection2Length += Utils.GetPaddingSize(subsection2Length);
            return subsection2Length;
        }

    }
}