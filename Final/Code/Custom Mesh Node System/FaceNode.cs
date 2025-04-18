using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class FaceNode : Node3D
{
    public List<EdgeNode> Edges { get; private set; } = new();

    private ArrayMesh _mesh = new ArrayMesh();
    private MeshInstance3D _meshInstance;
    private StandardMaterial3D _material;

    private bool _isFlipped = false;

    public Color FaceColor
    {
        get => _material.AlbedoColor;
        set
        {
            _material.AlbedoColor = value;
            _meshInstance.MaterialOverride = _material;
        }
    }

    public override void _Ready()
    {
        _meshInstance = new MeshInstance3D
        {
            Mesh = _mesh
        };

        _material = new StandardMaterial3D
        {
            AlbedoColor = Colors.DarkSlateBlue,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };

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

    public void Flip()
    {
        _isFlipped = !_isFlipped;
        UpdateFace();
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
                var v2 = new Vector2(p.X, p.Y);
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
     
        /*
          if (indices.Length < 3)
        {
            GD.PrintErr("[FaceNode] Triangulation failed.");
            return;
        }
         */
       

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        if (_isFlipped)
        {
            for (int i = 0; i < indices.Length; i += 3)
            {
                st.AddVertex(points3D[indices[i]]);
                st.AddVertex(points3D[indices[i + 2]]);
                st.AddVertex(points3D[indices[i + 1]]);
            }
        }
        else
        {
            for (int i = 0; i < indices.Length; i++)
                st.AddVertex(points3D[indices[i]]);
        }

        _mesh.ClearSurfaces();
        st.Commit(_mesh);
    }
}
