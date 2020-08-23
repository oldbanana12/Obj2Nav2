using System;
using System.Globalization;

namespace Obj2Nav2.WavefrontObj
{
    public class Vertex : IEquatable<Vertex>
    {
        public const int MinimumDataLength = 4;
        public const string Prefix = "v";

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public int Index { get; set; }

        public bool Equals(Vertex other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Vertex);
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

        public Vertex()
        {

        }

        public Vertex(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void LoadFromStringArray(string[] data)
        {
            if (data.Length < MinimumDataLength)
                throw new ArgumentException("Input array must be of minimum length " + MinimumDataLength, "data");

            if (!data[0].ToLower().Equals(Prefix))
                throw new ArgumentException("Data prefix must be '" + Prefix + "'", "data");

            bool success;

            double x, y, z;

            success = double.TryParse(data[1], NumberStyles.Any, CultureInfo.InvariantCulture, out x);
            if (!success) throw new ArgumentException("Could not parse X parameter as double");

            success = double.TryParse(data[2], NumberStyles.Any, CultureInfo.InvariantCulture, out y);
            if (!success) throw new ArgumentException("Could not parse Y parameter as double");

            success = double.TryParse(data[3], NumberStyles.Any, CultureInfo.InvariantCulture, out z);
            if (!success) throw new ArgumentException("Could not parse Z parameter as double");

            X = x;
            Y = y;
            Z = z;
        }
        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", X, Y, Z);
        }
    }
}
