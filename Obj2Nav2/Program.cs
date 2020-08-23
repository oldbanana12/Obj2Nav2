using Obj2Nav2.Nav2;
using Obj2Nav2.WavefrontObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Instrumentation;

namespace Obj2Nav2
{
    class GeometryEdge : IEquatable<GeometryEdge>
    {
        public Vertex A;
        public Vertex B;

        public bool Equals(GeometryEdge other)
        {
            return (other.A == A && other.B == B) || (other.B == A && other.A == B);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as GeometryEdge);
        }

        public override int GetHashCode()
        {
            int result = 37;

            result *= 397;
            result += A.GetHashCode();

            result *= 397;
            result += B.GetHashCode();

            return result;
        }

        public Vertex GetMidpoint ()
        {
            return new Vertex((A.X + B.X) / 2, (A.Y + B.Y) / 2, (A.Z + B.Z) / 2);
        }
    }

    class Face
    {
        public int Index;
        public int[] VertexIndexList;
        public GeometryEdge[] Edges;
        public Face[] AdjacentFaces;
        public Midpoint[] Midpoints;
    }

    public class Midpoint : IEquatable<Midpoint>
    {
        public ushort Index;
        public Vertex Position;

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Midpoint);
        }

        public bool Equals(Midpoint other)
        {
            return Position.Equals(other.Position);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (Path.GetExtension(args[0]) == ".obj")
                {
                    Obj obj = new Obj();
                    obj.LoadObj(args[0]);

                    Console.WriteLine("XSize: {0}, YSize: {1}, ZSize: {2}", obj.Size.XSize, obj.Size.YSize, obj.Size.ZSize);
                    Console.WriteLine("XMin: {0}, YMin: {1}, ZMin: {2}", obj.Size.XMin, obj.Size.YMin, obj.Size.ZMin);

                    var pieces = obj.Split(10, 10);
                    for (int i = 0; i < 10; i++)
                    {
                        for(int j = 0; j < 10; j++)
                        {
                            var file = new FileStream(string.Format("chunk{0}_{1}.obj", i, j), FileMode.Create);
                            pieces[i, j].SaveObj(file);
                        }
                    }

                    Nav2.Nav2 nav2 = new Nav2.Nav2
                    {
                        Origin = new Vector3(obj.Size.XMin, obj.Size.YMin, obj.Size.ZMin),
                        Size = new Vector3(obj.Size.XSize, obj.Size.YSize, obj.Size.ZSize)
                    };

                    NavworldEntry navworld = new NavworldEntry
                    {
                        GroupId = 4
                    };

                    var faces = new List<Face>();

                    ushort midpointIndex = 0;

                    ushort faceIndex = 0;
                    foreach (var objFace in obj.FaceList)
                    {
                        var face = new Face
                        {
                            Index = faceIndex,
                            VertexIndexList = new int[objFace.VertexIndexList.Count],
                            Edges = new GeometryEdge[objFace.VertexIndexList.Count],
                            AdjacentFaces = new Face[objFace.VertexIndexList.Count],
                            Midpoints = new Midpoint[objFace.VertexIndexList.Count]
                        };

                        for (int i = 0; i < objFace.VertexIndexList.Count; i++)
                        {
                            face.VertexIndexList[i] = objFace.VertexIndexList[i] - 1;
                            var a = obj.VertexList[objFace.VertexIndexList[i] - 1];
                            Vertex b;
                            if (i + 1 >= objFace.VertexIndexList.Count)
                            {
                                b = obj.VertexList[objFace.VertexIndexList[0] - 1];
                            }
                            else
                            {
                                b = obj.VertexList[objFace.VertexIndexList[i + 1] - 1];
                            }

                            face.Edges[i] = new GeometryEdge { A = a, B = b };
                            face.Midpoints[i] = new Midpoint
                            {
                                Index = midpointIndex,
                                Position = face.Edges[i].GetMidpoint()
                            };

                            midpointIndex++;

                            foreach (var otherFace in faces)
                            {
                                if (face == otherFace)
                                {
                                    continue;
                                }

                                for (int j = 0; j < otherFace.Edges.Length; j++)
                                {
                                    if (face.Edges[i].Equals(otherFace.Edges[j]))
                                    {
                                        face.AdjacentFaces[i] = otherFace;
                                        otherFace.AdjacentFaces[j] = face;
                                    }
                                }
                            }
                        }

                        faceIndex++;

                        faces.Add(face);
                    }

                    faceIndex = 0;
                    foreach (var face in faces)
                    {
                        for (int i = 0; i < face.Midpoints.Length; i++)
                        {
                            var position = new Vector3(face.Midpoints[i].Position.X, face.Midpoints[i].Position.Y, face.Midpoints[i].Position.Z);

                            var node = new NavworldNode
                            {
                                Position = new ScaledVertex(position, nav2.Origin, nav2.ScaleFactor),
                                Vec3Pos = position,
                                Index = face.Midpoints[i].Index,
                                FaceIndex = faceIndex
                            };

                            navworld.Points.Add(node);                            
                        }

                        faceIndex++;
                    }

                    foreach (var face in faces)
                    {
                        for (int i = 0; i < face.Midpoints.Length; i++)
                        {
                            if (face.AdjacentFaces[i] != null)
                            {
                                for (int k = 0; k < face.AdjacentFaces[i].Midpoints.Length; k++)
                                {
                                    if (face.AdjacentFaces[i].Midpoints[k].Equals(face.Midpoints[i]))
                                    {
                                        navworld.Points[face.Midpoints[i].Index].ZeroCostNode = navworld.Points[face.AdjacentFaces[i].Midpoints[k].Index];
                                    }
                                }
                            }

                            for (int j = 0; j < face.Midpoints.Length; j++)
                            {
                                if (i == j)
                                    continue;

                                navworld.Points[face.Midpoints[i].Index].AdjacentNodes.Add(navworld.Points[face.Midpoints[j].Index]);
                                navworld.Points[face.Midpoints[j].Index].AdjacentNodes.Add(navworld.Points[face.Midpoints[i].Index]);
                            }
                        }
                    }

                    nav2.Entries.Add(navworld);

                    var navmesh = new NavmeshChunkEntry();
                    navmesh.GroupId = 4;

                    foreach (var vertex in obj.VertexList)
                    {
                        var position = new Vector3(vertex.X, vertex.Y, vertex.Z);
                        navmesh.Vertices.Add(new ScaledVertex(position, nav2.Origin, nav2.ScaleFactor));
                    }

                    foreach (var face in faces)
                    {
                        var navmeshFace = new NavmeshFace();
                        navmeshFace.AdjacentFaces = new int[face.AdjacentFaces.Length];
                        navmeshFace.VertexIndices = new int[face.VertexIndexList.Length];
                        for (int i = 0; i < face.AdjacentFaces.Length; i++)
                        {
                            if (face.AdjacentFaces[i] == null)
                            {
                                navmeshFace.AdjacentFaces[i] = -1;
                                continue;
                            }

                            navmeshFace.AdjacentFaces[i] = face.AdjacentFaces[i].Index;
                        }

                        for (int i = 0; i < face.VertexIndexList.Length; i++)
                        {
                            navmeshFace.VertexIndices[i] = face.VertexIndexList[i];
                        }

                        navmesh.Faces.Add(navmeshFace);
                    }

                    nav2.Entries.Add(navmesh);

                    var segmentChunk = new SegmentChunkEntry();
                    segmentChunk.GroupId = 4;
                    segmentChunk.verts = (byte)obj.VertexList.Count;
                    segmentChunk.faces = (byte)faces.Count;
                    // TODO: Tell segment chunk about the edge count
                    nav2.Entries.Add(segmentChunk);

                    var segmentGraph = new SegmentGraphEntry();
                    segmentGraph.GroupId = 4;
                    nav2.Entries.Add(segmentGraph);

                    var navSystem = new NavSystem()
                    {
                        Origin = new Vector3(obj.Size.XMin, obj.Size.YMin, obj.Size.ZMin),
                        Size = new Vector3(obj.Size.XSize, obj.Size.YSize, obj.Size.ZSize)
                    };

                    nav2.navSystem = navSystem;

                    nav2.WriteNav2File("output.nav2");
                }
            }

        }
    }
}
