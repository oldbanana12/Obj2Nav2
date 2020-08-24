using Obj2Nav2.Nav2;
using Obj2Nav2.WavefrontObj;
using System.IO;

namespace Obj2Nav2
{
    class Program
    {
        private const uint WIDTH_CHUNKS = 10;
        private const uint HEIGHT_CHUNKS = 10;

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (Path.GetExtension(args[0]) == ".obj")
                {
                    Obj obj = new Obj();
                    obj.LoadObj(args[0]);

                    var pieces = obj.Split(WIDTH_CHUNKS, HEIGHT_CHUNKS);

                    Nav2.Nav2.Origin = new Vector3(obj.Size.XMin, obj.Size.YMin, obj.Size.ZMin);
                    Nav2.Nav2.Size = new Vector3(obj.Size.XSize, obj.Size.YSize, obj.Size.ZSize);
                    Nav2.Nav2 nav2 = new Nav2.Nav2();

                    nav2.LoadFromChunks(pieces, WIDTH_CHUNKS, HEIGHT_CHUNKS);
                    nav2.WriteNav2File("output.nav2");
                }
            }

        }
    }
}
