// See https://aka.ms/new-console-template for more information

using RvmSharp;
using RvmSharp.Containers;
using RvmSharp.Exporters;
using RvmSharp.Operations;
using RvmSharp.Primitives;
using RvmSharp.Tessellation;
using System.Drawing;
using System.Numerics;

public static class Program
{
    public static void Main()
    {
        var tolerance = 0.1f;
        var outputFilename = "/Users/ESJO/robot/rvmsharp/RvmFilter/out2.obj";
        var bounds = (new Vector3(360, 300, 50), new Vector3(363, 318, 60));

        var files = Directory.GetFiles("/Users/ESJO/Downloads/cast_models").Where(a => a.ToLower().EndsWith(".rvm"))
            .ToArray();

        var rvmFiles = files.Select(f => RvmParser.ReadRvm(File.OpenRead(f))).ToArray();


        var leafs = rvmFiles.SelectMany(rvm => rvm.Model.Children.SelectMany(CollectGeometryNodes)).ToArray();
        var totalLeafs = leafs.Length;

        var meshes = leafs
            .AsParallel()
            .Select(leaf =>
            {
                var tessellatedMeshes = TessellatorBridge.Tessellate(leaf, tolerance);
                //Console.WriteLine($"v {tessellatedMeshes[0].Item1.Vertices[0]}");
                var filteredMeshes = tessellatedMeshes.Where(m => m.Item1.IsInBounds(bounds)).ToArray();
                return (name: leaf.Name, primitives: filteredMeshes);
            })
            .ToArray();

        var totalMeshes = meshes.Length;

        using var objExporter = new ObjExporter(outputFilename);

        Color? previousColor = null;
        foreach ((string objectName, (RvmMesh, Color)[] primitives) in meshes)
        {
            objExporter.StartObject(objectName);
            objExporter.StartGroup(objectName);

            foreach ((RvmMesh? mesh, Color color) in primitives)
            {
                if (previousColor != color)
                    objExporter.StartMaterial(color);
                objExporter.WriteMesh(mesh);
                previousColor = color;
            }
        }
    }

    private static bool IsInBounds(this RvmMesh mesh, (Vector3 min, Vector3 max) b)
    {
        return mesh.Vertices.Any(v =>
            b.min.X < v.X && v.X < b.max.X &&
            b.min.Y < v.Y && v.Y < b.max.Y &&
            b.min.Z < v.Z && v.Z < b.max.Z
        );
    }

    private static IEnumerable<RvmNode> CollectGeometryNodes(RvmNode root)
    {
        if (root.Children.OfType<RvmPrimitive>().Any())
            yield return root;
        foreach (var geometryNode in root.Children.OfType<RvmNode>().SelectMany(CollectGeometryNodes))
            yield return geometryNode;
    }
}
