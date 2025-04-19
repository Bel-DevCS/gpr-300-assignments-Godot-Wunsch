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

    private MeshBuilderUI _meshBuilderUI;
    
    private List<PointNode> _loopSelection = new();
    
    private List<Shape> _shapes = new();
    public IReadOnlyList<Shape> Shapes => _shapes;


    public IReadOnlyList<PointNode> SelectedLoop => _loopSelection;
    
    private bool _wireframeEnabled = false;

    
    public void ClearLoopSelection()
    {
        foreach (var p in _loopSelection)
            p.SetColor(new Color(1, 0, 0)); // back to red
        _loopSelection.Clear();
    }


    public override void _Ready()
    {
        _meshInstance = GetNode<MeshInstance3D>("LiveMesh");
        _meshInstance.Mesh = _mesh;
        _gizmo = new Gizmo3D { Mode = Gizmo3D.ToolMode.Move };
        AddChild(_gizmo);

        _meshBuilderUI = new MeshBuilderUI(this);
        GenerateMeshFromChildren();
    }


    public override void _Process(double delta)
    {
        UpdateSelectedPointFromGizmo();
        ImGuiStyle.ApplyDarkModelingTheme();
        _meshBuilderUI.Draw();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            DeselectPoint();
            ClearLoopSelection(); // also clear shift-loop
        }

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            TrySelectPoint(mouseEvent.Position, Input.IsKeyPressed(Key.Shift));
        }
    }

    public void TrySelectPoint(Vector2 mousePos, bool isShiftClick = false)
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
                    if (isShiftClick)
                    {
                        if (!_loopSelection.Contains(point))
                        {
                            _loopSelection.Add(point);
                            point.SetColor(Colors.Cyan);
                        }
                    }
                    else
                    {
                        SelectPoint(point);
                    }
                    return;
                }
            }
        }
    }


    public void SelectPoint(PointNode point)
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

    public void DeselectPoint()
    {
        if (_selectedPoint != null)
            _gizmo.Deselect(_selectedPoint);

        _selectedPoint = null;
    }

    public void UpdateSelectedPointFromGizmo()
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
    
    public bool FaceExists(FaceNode candidate)
    {
        var candidateEdges = candidate.Edges.ToHashSet();

        foreach (var face in _faces)
        {
            if (face.Edges.Count != candidateEdges.Count)
                continue;

            // Check if all edges match (unordered)
            if (face.Edges.All(e => candidateEdges.Contains(e)))
                return true;
        }

        return false;
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
        var shape = CreateShape("Triangle");

        Vector3 a = center + new Vector3(0, size, 0);
        Vector3 b = center + new Vector3(-size * 0.866f, -size * 0.5f, 0);
        Vector3 c = center + new Vector3(size * 0.866f, -size * 0.5f, 0);

        var p0 = CreatePoint(a);
        var p1 = CreatePoint(b);
        var p2 = CreatePoint(c);

        AddToShape(shape, p0);
        AddToShape(shape, p1);
        AddToShape(shape, p2);

        AddEdge(p0, p1);
        AddEdge(p1, p2);
        AddEdge(p2, p0);

        var newEdges = _edges.Where(e =>
            (e.PointA == p0 || e.PointA == p1 || e.PointA == p2) &&
            (e.PointB == p0 || e.PointB == p1 || e.PointB == p2)).ToList();

        foreach (var edge in newEdges)
            AddToShape(shape, edge);

        AutoGenerateFace();

        var newFace = _faces.LastOrDefault();
        if (newFace != null)
            AddToShape(shape, newFace);
    }

    public void GenerateSquare(Vector3 center, float size = 1f)
    {
        var shape = CreateShape("Square");

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

        AddToShape(shape, p0);
        AddToShape(shape, p1);
        AddToShape(shape, p2);
        AddToShape(shape, p3);

        TryAddEdge(p0, p1);
        TryAddEdge(p1, p2);
        TryAddEdge(p2, p3);
        TryAddEdge(p3, p0);

        var newEdges = _edges.Where(e =>
            (shape.Points.Contains(e.PointA) && shape.Points.Contains(e.PointB))).ToList();

        foreach (var edge in newEdges)
            AddToShape(shape, edge);

        AutoGenerateFace();

        var newFace = _faces.LastOrDefault();
        if (newFace != null)
            AddToShape(shape, newFace);
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
        
        DeselectPoint();

        _edges.Clear();
        _faces.Clear();
    }


    public List<PointNode> GetPoints() => GetChildren().OfType<PointNode>().ToList();
    public List<EdgeNode> GetEdges() => _edges;
    public List<FaceNode> GetFaces() => _faces;

    public PointNode SelectedPoint => _selectedPoint;

    public void AddFace(FaceNode face)
    {
        AddChild(face);
        _faces.Add(face);
    }

    public bool IsLoopClosed()
    {
        if (_loopSelection.Count < 3)
            return false;

        for (int i = 0; i < _loopSelection.Count; i++)
        {
            var a = _loopSelection[i];
            var b = _loopSelection[(i + 1) % _loopSelection.Count];

            if (!EdgeExists(a, b))
                return false;
        }

        return true;
    }

    public void AddToLoopSelection(PointNode point)
    {
        if (!_loopSelection.Contains(point))
        {
            _loopSelection.Add(point);
            point.SetColor(Colors.Cyan);
        }
    }
    
    public void SetWireframeEnabled(bool enabled)
    {
        _wireframeEnabled = enabled;

        // Set global debug draw
        RenderingServer.SetDebugGenerateWireframes(true);
        GetViewport().DebugDraw = enabled
            ? Viewport.DebugDrawEnum.Wireframe
            : Viewport.DebugDrawEnum.Disabled;

        // Hide or show edges accordingly
        foreach (var edge in _edges)
            edge.SetEdgeVisible(!enabled);
    }
    
    private bool _isEditing = true;
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            _isEditing = value;
            foreach (var point in GetPoints())
                point.ShowDebug = _isEditing;
        }
    }

    public Shape CreateShape(string name = "Shape")
    {
        var shape = new Shape(name);
        _shapes.Add(shape);
        return shape;
    }

    public void AddToShape(Shape shape, Node3D node)
    {
        switch (node)
        {
            case PointNode point:
                shape.AddPoint(point);
                break;
            case EdgeNode edge:
                shape.AddEdge(edge);
                break;
            case FaceNode face:
                shape.AddFace(face);
                break;
            default:
                GD.PrintErr("Unsupported node type for shape.");
                break;
        }
    }

    public void TransformShape(Shape shape, Transform3D transform)
    {
        shape.ApplyTransform(transform);
    }
    
    public void ChangeParentShape(PointNode point, Shape newShape)
    {
        if (point == null || newShape == null)
            return;

        // Remove from old shape
        if (point.ParentShape != null)
        {
            point.ParentShape.Points.Remove(point);
        }

        // Add to new shape
        newShape.AddPoint(point);
        point.ParentShape = newShape;
    }


}
