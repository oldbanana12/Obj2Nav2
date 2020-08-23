using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obj2Nav2.WavefrontObj
{
    class Face
    {
        public const int MinimumDataLength = 4;
        public const string Prefix = "f";

        public List<int> VertexIndexList { get; set; }

        public Face()
        {
            VertexIndexList = new List<int>();
        }

        public void LoadFromStringArray(string[] data)
        {
            if (data.Length < MinimumDataLength)
                throw new ArgumentException("Input array must be of minimum length " + MinimumDataLength, "data");

            if (!data[0].ToLower().Equals(Prefix))
                throw new ArgumentException("Data prefix must be '" + Prefix + "'", "data");

            int vcount = data.Count() - 1;
            VertexIndexList = new List<int> (vcount);

            bool success;

            for (int i = 0; i < vcount; i++)
            {
                string[] parts = data[i + 1].Split('/');

                int vindex;
                success = int.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out vindex);
                if (!success) throw new ArgumentException("Could not parse parameter as int");
                VertexIndexList.Add (vindex);
            }
        }

        public Face[] Triangulate()
        {
            var faces = new Face[VertexIndexList.Count - 2];
            for (int i = 0; i < VertexIndexList.Count - 2; i++)
            {
                faces[i] = new Face();
                faces[i].VertexIndexList.Add(VertexIndexList[0]);
                faces[i].VertexIndexList.Add(VertexIndexList[i + 1]);
                faces[i].VertexIndexList.Add(VertexIndexList[i + 2]);
            }
            return faces;
        }

    }
}
