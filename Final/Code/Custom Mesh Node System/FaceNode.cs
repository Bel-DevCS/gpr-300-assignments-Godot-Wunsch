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
        if (Edges.Count < 2)
            return;

        SurfaceTool st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.SetSmoothGroup(0);

        int edgeCount = Edges.Count;

        for (int i = 0; i < edgeCount; i++)
        {
            var a = Edges[i].Line.Points;
            var b = Edges[(i + 1) % edgeCount].Line.Points;

            int ia = 0, ib = 0;

            while (ia < a.Count - 1 || ib < b.Count - 1)
            {
                Vector3 a1 = a[ia];
                Vector3 a2 = (ia + 1 < a.Count) ? a[ia + 1] : a1;
                Vector3 b1 = b[ib];
                Vector3 b2 = (ib + 1 < b.Count) ? b[ib + 1] : b1;

                float lenA = (a2 - a1).LengthSquared();
                float lenB = (b2 - b1).LengthSquared();

                if ((ia + 1 < a.Count && (ib + 1 == b.Count || lenA < lenB)))
                {
                    st.AddVertex(a1);
                    st.AddVertex(b1);
                    st.AddVertex(a2);
                    ia++;
                }
                else
                {
                    st.AddVertex(a1);
                    st.AddVertex(b1);
                    st.AddVertex(b2);
                    ib++;
                }
            }
        }

        _mesh.ClearSurfaces();
        st.Commit(_mesh);
    }
    
    public void UpdateFace()
    {
        GenerateFaceMesh();
    }

}
