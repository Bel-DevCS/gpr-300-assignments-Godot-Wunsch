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
        // Escape to deselect
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            DeselectPoint();
        }

        // Left-click for gizmo selection
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
        // Deselect old
        DeselectPoint();

        // Select new
        _selectedPoint = point;
        _dragStartPos = point.Position;
        _gizmo.GlobalTransform = point.GlobalTransform;
        _gizmo.Select(point);

        // Auto-assign PointA in Edge Linker
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
            _selectedPoint.Position = _selectedPoint.GlobalPosition;

            // Update all edges that reference the selected point
            foreach (var edge in _edges)
            {
                if (edge.PointA == _selectedPoint || edge.PointB == _selectedPoint)
                    edge.UpdateEdge();
            }
        }
    }

    // === ImGui UI ===
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

        ImGui.End();
    }

    // === Point/Edge/Face Creation ===
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
        var edge = new EdgeNode();
        edge.SetPoints(a, b);
        AddChild(edge);
        _edges.Add(edge);
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
}
