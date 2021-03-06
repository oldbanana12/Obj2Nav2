﻿using Obj2Nav2.WavefrontObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obj2Nav2.Nav2
{
    class Nav2
    {
        private const uint MAGIC = 201403242;
        private const uint HEADER_LENGTH = 96;
        private const uint MANIFEST_ENTRY_SIZE = 12;

        public List<IEntry> Entries;
        public static Vector3 Origin;

        public NavSystem navSystem;

        private static Vector3 _size;
        public static Vector3 Size {
            get
            {
                return _size;
            }

            set {
                _size = value;

                var xDivisor = (ushort)(65535 / Size.X);
                var yDivisor = (ushort)(65535 / Size.Y);
                var zDivisor = (ushort)(65535 / Size.Z);

                if (yDivisor == 0)
                {
                    yDivisor = 1;
                }

                ScaleFactor = new ScaledVertex(xDivisor, yDivisor, zDivisor);
            } 
        }

        public static ScaledVertex ScaleFactor;

        public Nav2()
        {
            Entries = new List<IEntry>();
        }

        public void WriteNav2File(string path)
        {
            using (var outStream = File.Create(path))
            using (var writer = new BinaryWriter(outStream))
            {
                writeHeader(writer);
                writeManifest(writer);
                writeEntries(writer);
                if (navSystem != null)
                {
                    navSystem.Write(writer);
                }
            }
        }

        private void writeHeader(BinaryWriter writer)
        {
            writer.Write(MAGIC);
            writer.Write(getFileSize());
            writer.Write((uint)(HEADER_LENGTH + getManifestTotalLength()));
            writer.Write((uint)Entries.Count);

            if (navSystem != null)
            {
                uint navSystemOffset = HEADER_LENGTH + getManifestTotalLength();
                foreach (var entry in Entries)
                {
                    navSystemOffset += entry.GetLength();
                }

                writer.Write((uint)navSystemOffset);
            }
            else
            {
                writer.Write((uint)0);
            }


            // TODO: Support split files
            writer.Write((byte)0);

            // TODO: Support writing 2D grid reference things
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);

            // TODO: Figure out WTF section 2 is and support it
            writer.Write((uint)0); // 0 offset for section 2

            writer.Write((uint)0); // Always 0?

            Origin.WriteToStream(writer);

            writer.Write((uint)0); // Unknown
            writer.Write((uint)0); // Unknown

            writer.Write(HEADER_LENGTH); // Manifest offset
            writer.Write((uint)(MANIFEST_ENTRY_SIZE * getManifestCount())); // Manifest length

            writer.Write((uint)0); // Unknown
            writer.Write((uint)0); // Unknown

            ScaleFactor.Write(writer);

            writer.Write((ushort)127); // Unknown for now, some sort of width maybe?
            writer.Write((byte)1); // Unknown

            writer.Write((byte)0); // TODO: Section 2 entry count

            writer.Write((ushort)0); // Unknown

            // TODO: Figure out what this load of values is, possibly generation parameters
            writer.Write(0x1A2D0845);
            writer.Write(0x46D9C1A9);
            writer.Write(0xA50A3838);
            writer.Write(0xB7A71CCD);
        }

        internal void LoadFromChunks(Obj[,] pieces, uint width_chunks, uint height_chunks)
        {
            NavworldEntry navworld = new NavworldEntry
            {
                GroupId = 4
            };

            navworld.LoadFromChunks(pieces, width_chunks, height_chunks);
            Entries.Add(navworld);

            NavmeshChunkEntry navmesh_chunk = new NavmeshChunkEntry
            {
                GroupId = 4
            };

            navmesh_chunk.LoadFromChunks(pieces, width_chunks, height_chunks);
            Entries.Add(navmesh_chunk);

            SegmentChunkEntry segment_chunk = new SegmentChunkEntry
            {
                GroupId = 4
            };

            segment_chunk.LoadFromChunks(pieces, width_chunks, height_chunks);

            Entries.Add(segment_chunk);

            SegmentGraphEntry segment_graph = new SegmentGraphEntry
            {
                GroupId = 4
            };

            segment_graph.LoadFromNavworld(navworld, width_chunks, height_chunks);

            Entries.Add(segment_graph);

            navSystem = new NavSystem();
            navSystem.Origin = Origin;
            navSystem.Size = Size;
            navSystem.ChunkIDs = segment_graph.GetChunkIDs();
        }

        private void writeManifest(BinaryWriter writer)
        {
            writer.Write((uint)Math.Ceiling((double)getManifestCount() / 3));

            var payloadOffset = HEADER_LENGTH + getManifestTotalLength() + 16;

            foreach (var entry in Entries)
            {
                if (entry.GetEntryType() == EntryType.SEGMENT_CHUNK)
                {
                    payloadOffset += entry.GetLength();
                    continue;
                }

                writer.Write(entry.GetGroupId());
                writer.Write((byte)0);
                writer.Write((ushort)0);
                writer.Write((uint)payloadOffset);
                writer.Write((byte)entry.GetEntryType());

                writer.Write((ushort)entry.GetLength());
                writer.Write((byte)0); // Probably padding

                payloadOffset += entry.GetLength();
            }

            uint padding = getManifestPaddingLength();
            for(uint i = 0; i < padding; i++)
            {
                writer.Write((byte)0x00);
            }
        }

        private void writeEntries(BinaryWriter writer)
        {
            foreach (var entry in Entries)
            {
                entry.Write(writer);
            }
        }

        private uint getFileSize()
        {
            uint length = HEADER_LENGTH + getManifestTotalLength();
            foreach (var entry in Entries)
            {
                length += entry.GetLength();
            }

            if (navSystem != null)
            {
                length += navSystem.GetLength();
            }

            return length;
        }

        private uint getManifestDataLength()
        {
            // 4 for length header + 12 bytes per entry
            return (uint)(sizeof(uint) + (MANIFEST_ENTRY_SIZE * getManifestCount()));
        }

        private uint getManifestPaddingLength()
        {
            return Utils.GetPaddingSize(getManifestDataLength());
        }

        private uint getManifestTotalLength()
        {
            return getManifestDataLength() + getManifestPaddingLength();
        }

        // Excludes SegmentChunks
        private uint getManifestCount ()
        {
            uint count = 0;
            foreach (var entry in Entries)
            {
                if (entry.GetEntryType() != EntryType.SEGMENT_CHUNK)
                    count++;
            }

            return count;
        }
    }
}
