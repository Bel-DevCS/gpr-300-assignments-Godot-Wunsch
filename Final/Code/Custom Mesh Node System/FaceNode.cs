using Godot;
using System.Collections.Generic;

[Tool]
public partial class FaceNode : Node3D
{
    public List<EdgeNode> Edges { get; private set; } = new();
    
    private ArrayMesh _mesh = new ArrayMesh();
    private MeshInstance3D _meshInstance;

    public override void _Ready()
    {
        _meshInstance = new MeshInstance3D();
        _meshInstance.Mesh = _mesh;
        _meshInstance.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = Colors.DarkSlateBlue,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };

        AddChild(_meshInstance);
        GenerateFaceMesh();
    }

    public void AddEdge(EdgeNode edge)
    {
        if (!Edges.Contains(edge))
        {
            Edges.Add(edge);
            edge.RegisterFace(this);
            UpdateFace();
        }
    }


    public void ClearEdges()
    {
        Edges.Clear();
        GenerateFaceMesh();
    }

    public void GenerateFaceMesh()
    {
        if (Edges.Count < 3) return;

        var points3D = new List<Vector3>();
        var points2D = new List<Vector2>();
        var seen = new HashSet<Vector2>();

        foreach (var edge in Edges)
        {
            foreach (var p in edge.Line.Points)
            {
                var v2 = new Vector2(p.X, p.Y); // or .XZ
                v2 = new Vector2(Mathf.Round(v2.X * 1000f) / 1000f, Mathf.Round(v2.Y * 1000f) / 1000f);

                if (seen.Add(v2))
                {
                    points3D.Add(p);
                    points2D.Add(v2);
                }
            }
        }

        if (points2D.Count < 3) return;

        var indices = Geometry2D.TriangulatePolygon(points2D.ToArray());

        if (indices.Length < 3)
        {
            GD.PrintErr("[FaceNode] Triangulation failed.");
            return;
        }

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        foreach (int i in indices)
            st.AddVertex(points3D[i]);

        _mesh.ClearSurfaces();
        st.Commit(_mesh);
    }

    public void UpdateFace()
    {
        GenerateFaceMesh();
    }

}
