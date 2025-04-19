using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class FaceNode : Node3D
{
    public List<EdgeNode> Edges { get; private set; } = new();

    private MeshInstance3D _meshInstance;

    private StandardMaterial3D _material = new StandardMaterial3D
    {
        AlbedoColor = Colors.DarkSlateBlue,
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled
    };

    public Color FaceColor
    {
        get => _material.AlbedoColor;
        set
        {
            _material.AlbedoColor = value;
            if (_meshInstance != null)
                _meshInstance.MaterialOverride = _material;
        }
    }



    public override void _Ready()
    {
        _meshInstance = new MeshInstance3D();
        _meshInstance.MaterialOverride = _material;
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

    public void UpdateFace()
    {
        GenerateFaceMesh();
    }

    public void GenerateFaceMesh()
    {
        if (Edges.Count < 3)
            return;

        var points3D = new List<Vector3>();
        var seen = new HashSet<Vector3>();

        foreach (var edge in Edges)
        {
            foreach (var p in edge.Line.Points)
            {
                Vector3 rounded = new Vector3(
                    Mathf.Round(p.X * 1000f) / 1000f,
                    Mathf.Round(p.Y * 1000f) / 1000f,
                    Mathf.Round(p.Z * 1000f) / 1000f
                );

                if (seen.Add(rounded))
                    points3D.Add(rounded);
            }
        }

        if (points3D.Count < 3)
            return;

        // Build plane from first 3 points
        Vector3 normal = (points3D[1] - points3D[0]).Cross(points3D[2] - points3D[0]).Normalized();
        Vector3 tangent = (points3D[1] - points3D[0]).Normalized();
        Vector3 bitangent = normal.Cross(tangent).Normalized();
        Vector3 origin = points3D[0];

        // Project 3D points to 2D using this basis
        List<Vector2> points2D = points3D.Select(p =>
        {
            Vector3 relative = p - origin;
            return new Vector2(relative.Dot(tangent), relative.Dot(bitangent));
        }).ToList();

        var indices = Geometry2D.TriangulatePolygon(points2D.ToArray());
        if (indices.Length < 3)
            return;

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        for (int i = 0; i < indices.Length; i++)
            st.AddVertex(points3D[indices[i]]);


        var newMesh = new ArrayMesh();
        st.Commit(newMesh);

        if (_meshInstance != null)
        {
            _meshInstance.Mesh = newMesh;
            _meshInstance.MaterialOverride = _material;
        }
        
    }

}
