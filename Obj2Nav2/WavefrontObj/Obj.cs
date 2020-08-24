using Obj2Nav2.Nav2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Obj2Nav2.WavefrontObj
{
    class Obj
    {
        public List<Vertex> VertexList;
        public List<Face> FaceList;

        public Extent Size;

        public Obj()
        {
            VertexList = new List<Vertex>();
            FaceList = new List<Face>();
        }

        public void LoadObj(string path) {
            LoadObj(File.ReadAllLines(path));
        }

        public void LoadObj(Stream data)
        {
            using (var reader = new StreamReader(data))
            {
                LoadObj(reader.ReadToEnd().Split(Environment.NewLine.ToCharArray()));
            }
        }

        public void LoadObj(IEnumerable<string> data)
        {
            foreach (var line in data)
            {
                processLine(line);
            }

            updateSize();
        }

        public void SaveObj(Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                foreach (var vertex in VertexList)
                {
                    writer.WriteLine("v {0} {1} {2}", vertex.X, vertex.Y, vertex.Z);
                }

                foreach (var face in FaceList)
                {
                    writer.WriteLine("f {0}", string.Join(" ", face.VertexIndexList));
                }
            }
        }

        private void updateSize()
        {
            if (VertexList.Count == 0)
            {
                Size = new Extent
                {
                    XMax = 0,
                    XMin = 0,
                    YMax = 0,
                    YMin = 0,
                    ZMax = 0,
                    ZMin = 0
                };

                return;
            }

            Size = new Extent
            {
                XMax = VertexList.Max(v => v.X),
                XMin = VertexList.Min(v => v.X),
                YMax = VertexList.Max(v => v.Y),
                YMin = VertexList.Min(v => v.Y),
                ZMax = VertexList.Max(v => v.Z),
                ZMin = VertexList.Min(v => v.Z)
            };
        }

        private void processLine(string line)
        {
            string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0)
            {
                switch (parts[0])
                {
                    case "v":
                        Vertex v = new Vertex();
                        v.LoadFromStringArray(parts);
                        VertexList.Add(v);
                        v.Index = VertexList.Count();
                        break;
                    case "f":
                        Face f = new Face();
                        f.LoadFromStringArray(parts);
                        FaceList.Add(f);
                        break;
                }
            }
        }

        public Obj[,] Split(uint numXChunks, uint numYChunks)
        {
            var pieces = new Obj[numXChunks, numYChunks];

            for (int i = 0; i < numXChunks; i++)
            {
                for (int j = 0; j < numYChunks; j++)
                {
                    pieces[i, j] = new Obj();
                }
            }

            if (numXChunks > 1)
            {
                Obj remainder = this;

                for (int pieceX = 0; pieceX < (numXChunks - 1); pieceX++)
                {
                    double boundary = Size.XMin + ((pieceX + 1) * (Size.XSize / numXChunks));

                    var halves = remainder.SplitVertical(boundary);
                    pieces[pieceX, 0] = halves[0];
                    remainder = halves[1];
                }

                pieces[numXChunks - 1, 0] = remainder;
            }

            if (numYChunks > 1)
            {
                for (int pieceX = 0; pieceX < numXChunks; pieceX++)
                {
                    Obj remainder = pieces[pieceX, 0];
                    for (int pieceY = 0; pieceY < (numYChunks - 1); pieceY++)
                    {
                        double boundary = Size.ZMin + ((pieceY + 1) * (Size.ZSize / numYChunks));

                        var halves = remainder.SplitHorizontal(boundary);
                        pieces[pieceX, pieceY] = halves[0];
                        remainder = halves[1];
                    }

                    pieces[pieceX, numYChunks - 1] = remainder;
                }
            }

            for(int i = 0; i < numXChunks; i++)
            {
                for (int j = 0; j < numYChunks; j++) {
                    var piece = pieces[i, j];
                    var newPiece = new Obj();
                    newPiece.VertexList.AddRange(piece.VertexList);

                    foreach (var face in piece.FaceList)
                    {
                        if (face.VertexIndexList.Count > 3)
                        {
                            var faces = face.Triangulate();
                            newPiece.FaceList.AddRange(faces);
                        } else
                        {
                            newPiece.FaceList.Add(face);
                        }
                    }

                    pieces[i, j] = newPiece;
                    pieces[i, j].updateSize();
                }
            }

            return pieces;
        }

        private Obj[] SplitVertical(double boundary)
        {
            var pieces = new Obj[2];
            pieces[0] = new Obj();
            pieces[1] = new Obj();

            foreach (var face in FaceList)
            {
                bool faceInLeft = true;
                bool faceInRight = true;
                List<Vertex> vertices = new List<Vertex>();
                foreach (var index in face.VertexIndexList)
                {
                    vertices.Add(VertexList[index - 1]);
                    if (VertexList[index - 1].X < boundary)
                    {
                        faceInRight = false;
                    }

                    if (VertexList[index - 1].X >= boundary)
                    {
                        faceInLeft = false;
                    }
                }

                if (faceInLeft && !faceInRight)
                {
                    var vertexIndexList = new List<int>();
                    foreach (var vertex in vertices)
                    {
                        vertexIndexList.Add(pieces[0].AddOrGetVertex(vertex));
                    }

                    var newFace = new Face();
                    newFace.VertexIndexList.AddRange(vertexIndexList);
                    pieces[0].FaceList.Add(newFace);
                }
                else if (faceInRight && !faceInLeft)
                {
                    var vertexIndexList = new List<int>();
                    foreach (var vertex in vertices)
                    {
                        vertexIndexList.Add(pieces[1].AddOrGetVertex(vertex));
                    }

                    var newFace = new Face();
                    newFace.VertexIndexList.AddRange(vertexIndexList);
                    pieces[1].FaceList.Add(newFace);
                }
                else if (!faceInLeft && !faceInRight)
                {
                    var leftVertices = new List<Vertex>();
                    var rightVertices = new List<Vertex>();

                    for (int i = 0; i < face.VertexIndexList.Count; i++)
                    {
                        var v1 = VertexList[face.VertexIndexList[i] - 1];
                        Vertex v2;
                        if (i + 1 >= face.VertexIndexList.Count)
                            v2 = VertexList[face.VertexIndexList[0] - 1];
                        else
                            v2 = VertexList[face.VertexIndexList[i + 1] - 1];

                        if (v1.X < boundary && v2.X < boundary)
                        {
                            leftVertices.Add(v1);
                            leftVertices.Add(v2);
                        }
                        else if (v1.X >= boundary && v2.X >= boundary)
                        {
                            rightVertices.Add(v1);
                            rightVertices.Add(v2);
                        }
                        else
                        {
                            var vec1 = new Vector3(v1.X, v1.Y, v1.Z);
                            var vec2 = new Vector3(v2.X, v2.Y, v2.Z);

                            var planePoint = new Vector3(boundary, 0, 0);
                            var planeNormal = new Vector3(boundary + 1, 0, 0);

                            if (v1.X < boundary)
                            {
                                leftVertices.Add(v1);
                                var second = Vector3.LinePlaneIntersectPoint(vec1 - vec2, vec1, planeNormal, planePoint);
                                leftVertices.Add(new Vertex(second.X, second.Y, second.Z));
                                rightVertices.Add(new Vertex(second.X, second.Y, second.Z));
                                rightVertices.Add(v2);
                            }
                            else if (v1.X >= boundary)
                            {
                                rightVertices.Add(v1);
                                var second = Vector3.LinePlaneIntersectPoint(vec1 - vec2, vec1, planeNormal, planePoint);
                                leftVertices.Add(new Vertex(second.X, second.Y, second.Z));
                                rightVertices.Add(new Vertex(second.X, second.Y, second.Z));
                                leftVertices.Add(v2);
                            }
                        }
                    }

                    var leftVertexIndexList = new HashSet<int>();
                    foreach (var vertex in leftVertices)
                    {
                        leftVertexIndexList.Add(pieces[0].AddOrGetVertex(vertex));
                    }

                    var newFace = new Face();
                    newFace.VertexIndexList.AddRange(leftVertexIndexList);
                    pieces[0].FaceList.Add(newFace);

                    var rightVertexIndexList = new HashSet<int>();
                    foreach (var vertex in rightVertices)
                    {
                        rightVertexIndexList.Add(pieces[1].AddOrGetVertex(vertex));
                    }

                    newFace = new Face();
                    newFace.VertexIndexList.AddRange(rightVertexIndexList);
                    pieces[1].FaceList.Add(newFace);
                }
            }

            return pieces;
        }

        private Obj[] SplitHorizontal(double boundary)
        {
            var pieces = new Obj[2];
            pieces[0] = new Obj();
            pieces[1] = new Obj();

            foreach (var face in FaceList)
            {
                bool faceInLeft = true;
                bool faceInRight = true;
                List<Vertex> vertices = new List<Vertex>();
                foreach (var index in face.VertexIndexList)
                {
                    vertices.Add(VertexList[index - 1]);
                    if (VertexList[index - 1].Z < boundary)
                    {
                        faceInRight = false;
                    }

                    if (VertexList[index - 1].Z >= boundary)
                    {
                        faceInLeft = false;
                    }
                }

                if (faceInLeft && !faceInRight)
                {
                    var vertexIndexList = new List<int>();
                    foreach (var vertex in vertices)
                    {
                        vertexIndexList.Add(pieces[0].AddOrGetVertex(vertex));
                    }

                    var newFace = new Face();
                    newFace.VertexIndexList.AddRange(vertexIndexList);
                    pieces[0].FaceList.Add(newFace);
                }
                else if (faceInRight && !faceInLeft)
                {
                    var vertexIndexList = new List<int>();
                    foreach (var vertex in vertices)
                    {
                        vertexIndexList.Add(pieces[1].AddOrGetVertex(vertex));
                    }

                    var newFace = new Face();
                    newFace.VertexIndexList.AddRange(vertexIndexList);
                    pieces[1].FaceList.Add(newFace);
                }
                else if (!faceInLeft && !faceInRight)
                {
                    var leftVertices = new List<Vertex>();
                    var rightVertices = new List<Vertex>();

                    for (int i = 0; i < face.VertexIndexList.Count; i++)
                    {
                        var v1 = VertexList[face.VertexIndexList[i] - 1];
                        Vertex v2;
                        if (i + 1 >= face.VertexIndexList.Count)
                            v2 = VertexList[face.VertexIndexList[0] - 1];
                        else
                            v2 = VertexList[face.VertexIndexList[i + 1] - 1];

                        if (v1.Z < boundary && v2.Z < boundary)
                        {
                            leftVertices.Add(v1);
                            leftVertices.Add(v2);
                        }
                        else if (v1.Z >= boundary && v2.Z >= boundary)
                        {
                            rightVertices.Add(v1);
                            rightVertices.Add(v2);
                        }
                        else
                        {
                            var vec1 = new Vector3(v1.X, v1.Y, v1.Z);
                            var vec2 = new Vector3(v2.X, v2.Y, v2.Z);

                            var planePoint = new Vector3(0, 0, boundary);
                            var planeNormal = new Vector3(0, 0, boundary + 1);

                            if (v1.Z < boundary)
                            {
                                leftVertices.Add(v1);
                                var second = Vector3.LinePlaneIntersectPoint(vec1 - vec2, vec1, planeNormal, planePoint);
                                leftVertices.Add(new Vertex(second.X, second.Y, second.Z));
                                rightVertices.Add(new Vertex(second.X, second.Y, second.Z));
                                rightVertices.Add(v2);
                            } 
                            else if (v1.Z >= boundary)
                            {
                                rightVertices.Add(v1);
                                var second = Vector3.LinePlaneIntersectPoint(vec1 - vec2, vec1, planeNormal, planePoint);
                                leftVertices.Add(new Vertex(second.X, second.Y, second.Z));
                                rightVertices.Add(new Vertex(second.X, second.Y, second.Z));
                                leftVertices.Add(v2);
                            }
                        }
                    }

                    var leftVertexIndexList = new HashSet<int>();
                    foreach (var vertex in leftVertices)
                    {
                        leftVertexIndexList.Add(pieces[0].AddOrGetVertex(vertex));
                    }

                    var newFace = new Face();
                    newFace.VertexIndexList.AddRange(leftVertexIndexList);
                    pieces[0].FaceList.Add(newFace);

                    var rightVertexIndexList = new HashSet<int>();
                    foreach (var vertex in rightVertices)
                    {
                        rightVertexIndexList.Add(pieces[1].AddOrGetVertex(vertex));
                    }

                    newFace = new Face();
                    newFace.VertexIndexList.AddRange(rightVertexIndexList);
                    pieces[1].FaceList.Add(newFace);
                }
            }

            return pieces;
        }

        private int AddOrGetVertex (Vertex v)
        {
            for (int i = 0; i < VertexList.Count; i++)
            {
                if (v.Equals(VertexList[i]))
                {
                    return i + 1;
                }
            }

            VertexList.Add(v);
            return VertexList.Count;
        }

    }
}
