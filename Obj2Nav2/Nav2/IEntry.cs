using System.IO;

namespace Obj2Nav2.Nav2
{
    public enum EntryType : ushort
    {
        NAVWORLD = 0,
        NAVMESH_CHUNK = 1,
        NAVWORLD_SEGMENT_GRAPH = 3,
        SEGMENT_CHUNK = 4,
    }

    public interface IEntry
    {
        uint GetLength();
        void Write(BinaryWriter writer);
        byte GetGroupId();
        EntryType GetEntryType();
    }
}