using System;

namespace Obj2Nav2.Nav2
{
    internal class NavworldEdge : IEquatable<NavworldEdge>
    {
        public ushort Index;
        public NavworldNode A;
        public NavworldNode B;

        public ushort Weight;

        public override bool Equals(object obj)
        {
            return Equals(obj as NavworldEdge);
        }

        public bool Equals(NavworldEdge other)
        {
            return (A == other.A && B == other.B) || (A == other.B && B == other.A);
        }
    }
}