using System;
using System.IO;

namespace Obj2Nav2.Nav2
{
    public class Vector3
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(double x, double y, double z)
        {
            X = (float)x;
            Y = (float)y;
            Z = (float)z;
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2}", X, Y, Z);
        }

        internal void WriteToStream(BinaryWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Z);
        }

        public static Vector3 operator +(Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
        }

        public static Vector3 operator -(Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z);
        }

        public static Vector3 operator *(Vector3 lhs, float rhs)
        {
            return new Vector3(lhs.X * rhs, lhs.Y * rhs, lhs.Z * rhs);
        }

        public float Dot(Vector3 rhs)
        {
            return X * rhs.X + Y * rhs.Y + Z * rhs.Z;
        }

        public Vector3 Cross(Vector3 rhs)
        {
            return new Vector3(
                Y * rhs.Z - Z * rhs.Y,
                Z * rhs.X - X * rhs.Z,
                X * rhs.Y - Y * rhs.X
            );
        }

        public static Vector3 LinePlaneIntersectPoint(Vector3 rayVector, Vector3 rayPoint, Vector3 planeNormal, Vector3 planePoint)
        {
            var diff = rayPoint - planePoint;
            var prod1 = diff.Dot(planeNormal);
            var prod2 = rayVector.Dot(planeNormal);
            var prod3 = prod1 / prod2;
            return rayPoint - rayVector * prod3;
        }
    }
}