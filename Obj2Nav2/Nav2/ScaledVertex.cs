using System;
using System.IO;

namespace Obj2Nav2.Nav2
{
    public class ScaledVertex
    {
        public ushort X;
        public ushort Y;
        public ushort Z;

        public ScaledVertex(ushort x, ushort y, ushort z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public ScaledVertex(Vector3 input, Vector3 origin, ScaledVertex scaleFactors)
        {
            X = (ushort)(Math.Abs(input.X - origin.X) * scaleFactors.X);
            Y = (ushort)(Math.Abs(input.Y - origin.Y) * scaleFactors.Y);
            Z = (ushort)(Math.Abs(input.Z - origin.Z) * scaleFactors.Z);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Z);
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2}", X, Y, Z);
        }

        public override bool Equals(object obj)
        {
            return X == (obj as ScaledVertex).X && Y == (obj as ScaledVertex).Y && Z == (obj as ScaledVertex).Z;
        }
        public override int GetHashCode()
        {
            int result = 37;

            result *= 397;
            result += X.GetHashCode();

            result *= 397;
            result += Y.GetHashCode();

            result *= 397;
            result += Z.GetHashCode();

            return result;
        }
    }
}
