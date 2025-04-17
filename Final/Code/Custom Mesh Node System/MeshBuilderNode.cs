using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Gizmo3DPlugin;

public partial class MeshBuilderNode : Node3D
{
    private ArrayMesh _mesh = new ArrayMesh();
    private MeshInstance3D _meshInstance;

    private List<EdgeNode> _edges = new();
    private List<FaceNode> _faces = new();

    private PointNode _selectedPoint = null;
    private EdgeNode _selectedEdge = null;

    private Vector3 _dragStartPos;
    private Gizmo3D _gizmo;

    private int _edgePointAIndex = 0;
    private int _edgePointBIndex = 1;

    public override void _Ready()
    {
        _meshInstance = GetNode<MeshInstance3D>("LiveMesh");
        _meshInstance.Mesh = _mesh;

        _gizmo = new Gizmo3D { Mode = Gizmo3D.ToolMode.Move };
        AddChild(_gizmo);

        GenerateMeshFromChildren();
    }

    public override void _Process(double delta)
    {
        UpdateSelectedPointFromGizmo();
        DrawEditor();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            DeselectPoint();
        }

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            TrySelectPoint(mouseEvent.Position);
        }
    }

    private void TrySelectPoint(Vector2 mousePos)
    {
        var camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        Vector3 origin = camera.ProjectRayOrigin(mousePos);
        Vector3 direction = camera.ProjectRayNormal(mousePos) * 1000f;

        var spaceState = GetViewport().GetWorld3D().DirectSpaceState;
        var result = spaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(origin, origin + direction));

        if (result.TryGetValue("collider", out var colliderVariant))
        {
            var collider = colliderVariant.AsGodotObject() as CollisionObject3D;
            foreach (var point in GetChildren().OfType<PointNode>())
            {
                if (collider.IsAncestorOf(point) || point.IsAncestorOf(collider))
                {
                    SelectPoint(point);
                    return;
                }
            }
        }
    }

    private void SelectPoint(PointNode point)
    {
        DeselectPoint();
        _selectedPoint = point;
        _dragStartPos = point.Position;
        _gizmo.GlobalTransform = point.GlobalTransform;
        _gizmo.Select(point);

        var points = GetChildren().OfType<PointNode>().ToList();
        int index = points.IndexOf(point);
        if (index >= 0)
            _edgePointAIndex = index;
    }

    private void DeselectPoint()
    {
        if (_selectedPoint != null)
            _gizmo.Deselect(_selectedPoint);

        _selectedPoint = null;
    }

    private void UpdateSelectedPointFromGizmo()
    {
        if (_selectedPoint != null)
        {
            // Update the actual point’s position based on gizmo
            _selectedPoint.Position = _selectedPoint.GlobalPosition;

            // Rebuild any edges connected to this point
            foreach (var edge in _edges)
            {
                if (edge.PointA == _selectedPoint || edge.PointB == _selectedPoint)
                {
                    edge.UpdateEdge();

                    // Rebuild any faces using this edge
                    foreach (var face in edge.ConnectedFaces)
                    {
                        face.UpdateFace();
                    }
                }
            }
        }
    }


    private void DrawEditor()
    {
        DrawPointEditor();
        DrawEdgeEditor();
        DrawFaceEditor();
        DrawMeshActions();
    }

    private void DrawPointEditor()
    {
        if (!ImGui.Begin("Point Editor")) return;

        foreach (var point in GetChildren().OfType<PointNode>())
        {
            if (ImGui.Selectable(point.Label, _selectedPoint == point))
                SelectPoint(point);
        }

        if (_selectedPoint != null)
        {
            var pos = _selectedPoint.Position;
            var np = new System.Numerics.Vector3(pos.X, pos.Y, pos.Z);
            if (ImGui.DragFloat3("Position", ref np))
                _selectedPoint.Position = new Vector3(np.X, np.Y, np.Z);
        }

        ImGui.End();
    }

    private void DrawEdgeEditor()
    {
        if (!ImGui.Begin("Edge Editor")) return;

        var points = GetChildren().OfType<PointNode>().ToList();
        var labels = points.Select(p => p.Label).ToArray();

        if (labels.Length >= 2)
        {
            ImGui.Combo("Point A", ref _edgePointAIndex, labels, labels.Length);
            ImGui.Combo("Point B", ref _edgePointBIndex, labels, labels.Length);

            if (ImGui.Button("Link Points"))
            {
                var a = points[_edgePointAIndex];
                var b = points[_edgePointBIndex];
                if (a != b)
                    AddEdge(a, b);
            }
        }
        else ImGui.Text("Need 2+ points.");

        ImGui.Separator();
        if (_edges.Count > 0)
        {
            for (int i = 0; i < _edges.Count; i++)
            {
                bool selected = (_selectedEdge == _edges[i]);
                if (ImGui.Selectable($"Edge {i}: {_edges[i].PointA.Label} → {_edges[i].PointB.Label}", selected))
                    _selectedEdge = _edges[i];

                _edges[i].SetHighlighted(selected);
            }


            if (_selectedEdge != null)
            {
                _selectedEdge.Line.DrawImGui(); 
                _selectedEdge.UpdateEdge();
                foreach (var face in _selectedEdge.ConnectedFaces)
                    face.UpdateFace();
            }
        }

        ImGui.End();
    }


    private void DrawFaceEditor()
    {
        if (!ImGui.Begin("Face Editor")) return;

        if (ImGui.Button("Add Face (All Edges)"))
        {
            var face = new FaceNode();
            foreach (var edge in _edges)
                face.AddEdge(edge);
            AddChild(face);
            _faces.Add(face);
        }

        if (ImGui.Button("Auto-Generate Face (Loop)"))
            AutoGenerateFace();

        ImGui.End();
    }

    private void DrawMeshActions()
    {
        if (!ImGui.Begin("Mesh Actions")) return;

        if (ImGui.Button("Add Point"))
            AddPoint(Vector3.Zero);

        if (ImGui.Button("Rebuild Mesh"))
            GenerateMeshFromChildren();

        if (ImGui.Button("Add Triangle"))
        {
            ClearMesh();
            GenerateTriangle(Vector3.Zero);
        }


        if (ImGui.Button("Add Square"))
        {
            ClearMesh();
            GenerateSquare(Vector3.Zero);
        }
            


        ImGui.End();
    }

    public void AddPoint(Vector3 pos)
    {
        var point = new PointNode
        {
            Position = pos,
            Label = $"P{GetChildren().OfType<PointNode>().Count()}"
        };
        AddChild(point);
    }

    public void AddEdge(PointNode a, PointNode b)
    {
        if (EdgeExists(a, b))
            return;

        var edge = new EdgeNode();
        edge.SetPoints(a, b);
        AddChild(edge);
        _edges.Add(edge);
    }

    
    public bool EdgeExists(PointNode a, PointNode b)
    {
        return _edges.Any(e =>
        {
            bool sameRef = (e.PointA == a && e.PointB == b) || (e.PointA == b && e.PointB == a);
            bool samePos = (e.PointA.Position == a.Position && e.PointB.Position == b.Position) ||
                           (e.PointA.Position == b.Position && e.PointB.Position == a.Position);
            return sameRef || samePos;
        });
    }



    public void GenerateMeshFromChildren()
    {
        var points = GetChildren().OfType<PointNode>().Select(p => p.GlobalPosition).ToList();
        _mesh.ClearSurfaces();
        if (points.Count < 3) return;

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var center = points.Aggregate(Vector3.Zero, (acc, p) => acc + p) / points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            st.AddVertex(center);
            st.AddVertex(a);
            st.AddVertex(b);
        }

        st.GenerateNormals();
        st.Commit(_mesh);
    }

    public void AutoGenerateFace()
    {
        if (_edges.Count < 3) return;

        var orderedEdges = new List<EdgeNode>();
        var remaining = new List<EdgeNode>(_edges);
        var start = remaining[0];

        orderedEdges.Add(start);
        remaining.RemoveAt(0);

        var endPoint = start.PointB;

        while (remaining.Count > 0)
        {
            var next = remaining.FirstOrDefault(e => e.PointA == endPoint);
            if (next == null) break;

            orderedEdges.Add(next);
            endPoint = next.PointB;
            remaining.Remove(next);
        }

        if (orderedEdges.Count >= 3 && endPoint == orderedEdges[0].PointA)
        {
            var face = new FaceNode();
            foreach (var edge in orderedEdges)
                face.AddEdge(edge);
            AddChild(face);
            _faces.Add(face);
        }
        else GD.Print("Not a closed loop.");
    }
    
    
    //
    public void GenerateTriangle(Vector3 center, float size = 1f)
    {
        Vector3 a = center + new Vector3(0, size, 0);
        Vector3 b = center + new Vector3(-size * 0.866f, -size * 0.5f, 0);
        Vector3 c = center + new Vector3(size * 0.866f, -size * 0.5f, 0);

        var p0 = CreatePoint(a);
        var p1 = CreatePoint(b);
        var p2 = CreatePoint(c);

        AddEdge(p0, p1);
        AddEdge(p1, p2);
        AddEdge(p2, p0);

        AutoGenerateFace();
    }

    public void GenerateSquare(Vector3 center, float size = 1f)
    {
        float half = size / 2f;

        Vector3 a = center + new Vector3(-half, half, 0);
        Vector3 b = center + new Vector3(half, half, 0);
        Vector3 c = center + new Vector3(half, -half, 0);
        Vector3 d = center + new Vector3(-half, -half, 0);

        int baseIndex = GetChildren().OfType<PointNode>().Count();

        var p0 = CreatePoint(a, baseIndex + 0);
        var p1 = CreatePoint(b, baseIndex + 1);
        var p2 = CreatePoint(c, baseIndex + 2);
        var p3 = CreatePoint(d, baseIndex + 3);

        TryAddEdge(p0, p1);
        TryAddEdge(p1, p2);
        TryAddEdge(p2, p3);
        TryAddEdge(p3, p0);

        AutoGenerateFace();
    }

    private void TryAddEdge(PointNode a, PointNode b)
    {
        if (EdgeExists(a, b))
            GD.Print($"[EdgeExists] Skipping edge {a.Label} → {b.Label}");
        else
            AddEdge(a, b);
    }


    private PointNode CreatePoint(Vector3 pos, int? index = null)
    {
        var count = index ?? GetChildren().OfType<PointNode>().Count();
        var point = new PointNode
        {
            Position = pos,
            Label = $"P{count}"
        };
        AddChild(point);
        return point;
    }

    public void ClearMesh()
    {
        foreach (var child in GetChildren().OfType<PointNode>().ToList())
            child.QueueFree();
        foreach (var child in _edges)
            child.QueueFree();
        foreach (var face in _faces)
            face.QueueFree();

        _edges.Clear();
        _faces.Clear();
    }


}
