using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using Gizmo3DPlugin;
using ImGuiNET;

public partial class CustomMeshObject : Node
{
    private ImmediateMesh _mesh = new ImmediateMesh();
    private MeshInstance3D _meshInstance;

    private List<ControlPoint> _points = new();
    private float _thickness = 0.1f;
    public bool _editingMode = true;
    private List<MeshInstance3D> _debugSpheres = new();
    private Gizmo3DPlugin.Gizmo3D _gizmo = new();
    private int _selectedPointIndex = -1;
    
    private Vector3 _dragStartPos;
    private int _dragIndex = -1;
    
    private int _pendingReorderFrom = -1;
    private int _pendingReorderTo = -1;
    
    private ControlPoint? _preImGuiEditSnapshot = null;
    
    private bool _editingInInspector = false;
    private float _inspectorEditCooldown = 0f;

    private int _runtimeUndoBaseline = 0;
    
    public List<ControlPoint> Points => _points;
    public List<MeshInstance3D> DebugSpheres => _debugSpheres;

    public int SelectedPointIndex { get => _selectedPointIndex; set => _selectedPointIndex = value; }
    public int PendingReorderFrom { get => _pendingReorderFrom; set => _pendingReorderFrom = value; }
    public int PendingReorderTo { get => _pendingReorderTo; set => _pendingReorderTo = value; }
    
    private CustomMeshObject_UI _ui;
    
    private List<LineBuilder> _edges = new();
    private List<MeshInstance3D> _edgeMeshes = new();

    
    
    public  struct ControlPoint
    {
        public string Label;
        public Vector3 Position;
        public Basis Rotation;
        public Vector3 Scale;

        public Transform3D ToTransform()
        {
            return new Transform3D(Rotation.Scaled(Scale), Position);
        }
        
    }

    public enum EditAction
    {
        Move,
        Rotate,
        Scale,
        Add,
        Remove,
        Clear
    }

    public struct PointEdit
    {
        public EditAction Action;
        public int Index;
        public ControlPoint State;
    }
    
    private PointEdit MakeEdit(EditAction action, int index)
    {
        index = Mathf.Clamp(index, 0, _points.Count - 1);
        return new PointEdit
        {
            Action = action,
            Index = index,
            State = _points[index]
        };
    }

    public override void _Ready()
    {
        _meshInstance = new MeshInstance3D();
        _meshInstance.Mesh = _mesh;
        var mat = new StandardMaterial3D { AlbedoColor = Colors.Blue, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
        _meshInstance.MaterialOverride = mat;
        AddChild(_meshInstance);

        AddPoint(new Vector3(0, 0, 0));
        AddPoint(new Vector3(1, 0, 0));
        AddPoint(new Vector3(2, 0, 1));

        AddChild(_gizmo);
        _gizmo.Mode = Gizmo3D.ToolMode.Move;
        
        _ui = new CustomMeshObject_UI(this);
        
        EditSystem.RegisterEditor(EditMode.MeshEditing, this);
        _runtimeUndoBaseline = EditSystem.GetUndoCount(EditMode.MeshEditing);
    }
    

    public override void _Process(double delta)
    {
        if (!_editingInInspector)
            UpdatePointsFromDebugSpheres();

        GenerateMeshFromPoints();
        GenerateEdgesFromPoints();

        _ui.DrawEditor();
    }
    private void UpdatePointsFromDebugSpheres()
    {
        for (int i = 0; i < _debugSpheres.Count; i++)
        {
            if (i >= _points.Count) continue;

            var sphere = _debugSpheres[i];
            if (sphere == null || !IsInstanceValid(sphere))
                continue;

            Transform3D xform = sphere.GlobalTransform;

            var old = _points[i]; // preserve label

            _points[i] = new ControlPoint
            {
                Label = old.Label,
                Position = xform.Origin,
                Rotation = xform.Basis.Orthonormalized(),
                Scale = GetScaleFromBasis(xform.Basis)
            };
        }
    }

    public override void _UnhandledInput(InputEvent @event)
{
    if (!_editingMode) return;

    if (@event is InputEventMouseButton mouseBtn)
    {
        if (mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            TrySelectSphere(mouseBtn.Position);
        }
        else if (!mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left && _selectedPointIndex != -1)
        {
            Vector3 newPos = _points[_selectedPointIndex].Position;

            if (_dragStartPos.DistanceSquaredTo(newPos) > 0.0001f)
            {
                var action = _gizmo.Mode switch
                {
                    Gizmo3D.ToolMode.Move => EditAction.Move,
                    Gizmo3D.ToolMode.Rotate => EditAction.Rotate,
                    Gizmo3D.ToolMode.Scale => EditAction.Scale,
                    _ => EditAction.Move
                };
                
                var previousState = new PointEdit
                {
                    Action = action,
                    Index = _selectedPointIndex,
                    State = new ControlPoint
                    {
                        Label = _points[_selectedPointIndex].Label,
                        Position = _dragStartPos,
                        Rotation = _points[_selectedPointIndex].Rotation,
                        Scale = _points[_selectedPointIndex].Scale
                    }
                };

                EditSystem.PushUndo(EditMode.MeshEditing, previousState);
                _dragStartPos = newPos;
            }
        }
    }

    if (@event is InputEventKey key && key.Pressed)
    {
        bool ctrl = Input.IsKeyPressed(Key.Ctrl);

        if (ctrl && key.Keycode == Key.Z)
            Undo();
        else if (ctrl && key.Keycode == Key.Y)
            Redo();
        else if (!ctrl)
        {
            switch (key.Keycode)
            {
                case Key.Key1: _gizmo.Mode = Gizmo3D.ToolMode.Move; break;
                case Key.Key2: _gizmo.Mode = Gizmo3D.ToolMode.Rotate; break;
                case Key.Key3: _gizmo.Mode = Gizmo3D.ToolMode.Scale; break;
                case Key.Escape: DeselectSphere(); break;
            }
        }
    }
}

    private void TrySelectSphere(Vector2 mousePos)
    {
        var camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        Vector3 origin = camera.ProjectRayOrigin(mousePos);
        Vector3 direction = camera.ProjectRayNormal(mousePos) * 1000f;

        var spaceState = GetViewport().GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(origin, origin + direction);
        var result = spaceState.IntersectRay(query);

        if (!result.TryGetValue("collider", out var colliderVariant)) return;

        var collider = colliderVariant.AsGodotObject() as CollisionObject3D;
        if (collider == null) return;

        for (int i = 0; i < _debugSpheres.Count; i++)
        {
            if (collider.IsAncestorOf(_debugSpheres[i]) || _debugSpheres[i].IsAncestorOf(collider))
            {
                DeselectSphere();
                _selectedPointIndex = i;
                AttachGizmoToSelected();
                _dragStartPos = _points[_selectedPointIndex].Position;
                return;
            }
        }
    }
    private void AttachGizmoToSelected()
    {
        for (int i = 0; i < _debugSpheres.Count; i++)
        {
            if (_debugSpheres[i].MaterialOverride is StandardMaterial3D mat)
                mat.AlbedoColor = (i == _selectedPointIndex) ? Colors.DarkGreen : Colors.DarkRed;
        }

        if (_selectedPointIndex >= 0 && _selectedPointIndex < _debugSpheres.Count)
        {
            var sphere = _debugSpheres[_selectedPointIndex];
            _gizmo.GlobalTransform = sphere.GlobalTransform;
            _gizmo.Select(sphere);
        }
    }
    private void DeselectSphere()
    {
        foreach (var sphere in _debugSpheres)
        {
            if (sphere.MaterialOverride is StandardMaterial3D mat)
                mat.AlbedoColor = Colors.DarkRed;
        }

        if (_selectedPointIndex >= 0 && _selectedPointIndex < _debugSpheres.Count)
        {
            _gizmo.Deselect(_debugSpheres[_selectedPointIndex]);
        }

        _selectedPointIndex = -1;
    }
    private Vector3 GetScaleFromBasis(Basis basis)
    {
        return new Vector3(
            basis.X.Length(),
            basis.Y.Length(),
            basis.Z.Length()
        );
    }
    public void GenerateMeshFromPoints()
    {
        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

        int count = _points.Count;

        if (count == 1)
        {
            Vector3 p = _points[0].Position;
            float size = _thickness;

            _mesh.SurfaceAddVertex(p + new Vector3(size, 0, 0));
            _mesh.SurfaceAddVertex(p + new Vector3(0, size, 0));
            _mesh.SurfaceAddVertex(p + new Vector3(0, 0, size));
        }
        else if (count == 2)
        {
            var a = _points[0].Position;
            var b = _points[1].Position;
            var up = Vector3.Up;
            var right = (b - a).Cross(up).Normalized() * _thickness;

            Vector3 p1 = a + right;
            Vector3 p2 = a - right;
            Vector3 p3 = b + right;
            Vector3 p4 = b - right;

            _mesh.SurfaceAddVertex(p1);
            _mesh.SurfaceAddVertex(p2);
            _mesh.SurfaceAddVertex(p3);

            _mesh.SurfaceAddVertex(p2);
            _mesh.SurfaceAddVertex(p4);
            _mesh.SurfaceAddVertex(p3);
        }
        else if (count >= 3)
        {
            // Center fan triangulation
            Vector3 center = Vector3.Zero;
            foreach (var pt in _points)
                center += pt.Position;
            center /= count;

            for (int i = 0; i < count; i++)
            {
                Vector3 a = _points[i].Position;
                Vector3 b = _points[(i + 1) % count].Position;

                _mesh.SurfaceAddVertex(center);
                _mesh.SurfaceAddVertex(a);
                _mesh.SurfaceAddVertex(b);
            }
        }

        _mesh.SurfaceEnd();
    }

    
    private void GenerateEdgesFromPoints()
    {
        // Clean up old edges
        foreach (var mesh in _edgeMeshes)
            if (IsInstanceValid(mesh))
                mesh.QueueFree();

        _edges.Clear();
        _edgeMeshes.Clear();

        if (_points.Count < 2)
            return;

        int edgeCount = _points.Count;

        for (int i = 0; i < edgeCount; i++)
        {
            int nextIndex = (i + 1) % edgeCount; // wraps around at end

            var line = new LineBuilder
            {
                StartPoint = _points[i].Position,
                EndPoint = _points[nextIndex].Position,
                CurveAmount = 0.0f // default; could later be per-edge
            };

            line.DrawLine();

            var mesh = line.CreateMeshInstance();
            AddChild(mesh);

            _edges.Add(line);
            _edgeMeshes.Add(mesh);
        }
    }


    
    private void AddPoint(Vector3 point, bool pushUndo = true)
    {
        var cp = new ControlPoint
        {
            Label = $"Point {_points.Count}",
            Position = point,
            Rotation = Basis.Identity,
            Scale = Vector3.One
        };

        _points.Add(cp);
        if (pushUndo)
            EditSystem.PushUndo(EditMode.MeshEditing, MakeEdit(EditAction.Add, _points.Count - 1));

        if (_editingMode) SpawnDebugSphere(point);
        
        GenerateMeshFromPoints();
        GenerateEdgesFromPoints(); 
    }

    private void DeleteSelectedPoint()
    {
        if (_selectedPointIndex < 0 || _selectedPointIndex >= _points.Count)
            return;

        // Store before deselecting (since it resets _selectedPointIndex)
        int indexToDelete = _selectedPointIndex;
        var deletedPoint = _points[indexToDelete];

        EditSystem.PushUndo(EditMode.MeshEditing, MakeEdit(EditAction.Remove, _selectedPointIndex));


        DeselectSphere(); // safe now

        _points.RemoveAt(indexToDelete);

        if (_editingMode && indexToDelete < _debugSpheres.Count)
        {
            var sphere = _debugSpheres[indexToDelete];
            if (IsInstanceValid(sphere))
                sphere.QueueFree();
            _debugSpheres.RemoveAt(indexToDelete);
        }
        
        GenerateMeshFromPoints();
        GenerateEdgesFromPoints();
    }
    private void ClearPoints()
    {
        _points.Clear();
        ClearDebugSpheres();
    }
    private void ClearDebugSpheres()
    {
        foreach (var sphere in _debugSpheres)
            if (IsInstanceValid(sphere))
                sphere.QueueFree();
        _debugSpheres.Clear();
    }
    private MeshInstance3D SpawnDebugSphere(Vector3 position)
    {
        var sphere = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.1f, Height = 0.2f, RadialSegments = 8, Rings = 8 },
            Position = position,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = Colors.DarkRed, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded }
        };

        var body = new StaticBody3D();
        var collider = new CollisionShape3D { Shape = new SphereShape3D { Radius = 0.1f } };
        body.AddChild(collider);
        body.Position = Vector3.Zero;
        sphere.AddChild(body);

        AddChild(sphere);
        _debugSpheres.Add(sphere);
        return sphere;
    }
    public void Undo()
    {
        if (EditSystem.GetUndoCount(EditMode.MeshEditing) <= _runtimeUndoBaseline)
        {
            GD.Print("[Undo] At baseline — cannot undo further.");
            return;
        }
        
        var obj = EditSystem.PopUndo(EditMode.MeshEditing);
        if (obj is not PointEdit edit) return;

        GD.Print($"[Undo] Undoing {edit.Action} at index {edit.Index}");

        switch (edit.Action)
        {
            case EditAction.Move:
            case EditAction.Rotate:
            case EditAction.Scale:
                if (edit.Index >= 0 && edit.Index < _points.Count)
                {
                    var current = _points[edit.Index];
                    _points[edit.Index] = edit.State;
                    UpdateDebugSphere(edit.Index);

                    EditSystem.PushRedo(EditMode.MeshEditing, new PointEdit
                    {
                        Action = edit.Action,
                        Index = edit.Index,
                        State = current
                    });
                }
                break;

            case EditAction.Add:
                if (edit.Index >= 0 && edit.Index < _points.Count)
                {
                    var removed = _points[edit.Index];
                    _points.RemoveAt(edit.Index);
                    if (_editingMode && edit.Index < _debugSpheres.Count)
                    {
                        _debugSpheres[edit.Index].QueueFree();
                        _debugSpheres.RemoveAt(edit.Index);
                    }

                    EditSystem.PushRedo(EditMode.MeshEditing, new PointEdit
                    {
                        Action = EditAction.Add,
                        Index = edit.Index,
                        State = removed
                    });
                }
                break;

            case EditAction.Remove:
                _points.Insert(edit.Index, edit.State);
                if (_editingMode)
                    _debugSpheres.Insert(edit.Index, SpawnDebugSphere(edit.State.Position));

                EditSystem.PushRedo(EditMode.MeshEditing, new PointEdit
                {
                    Action = EditAction.Remove,
                    Index = edit.Index,
                    State = edit.State
                });
                break;
        }

        GenerateMeshFromPoints();
        GenerateEdgesFromPoints();
    }



    public void Redo()
    {
        var obj = EditSystem.PopRedo(EditMode.MeshEditing);
        if (obj is not PointEdit edit) return;

        GD.Print($"[Redo] Redoing {edit.Action} at index {edit.Index}");

        switch (edit.Action)
        {
            case EditAction.Move:
            case EditAction.Rotate:
            case EditAction.Scale:
                if (edit.Index >= 0 && edit.Index < _points.Count)
                {
                    var current = _points[edit.Index];
                    _points[edit.Index] = edit.State;
                    UpdateDebugSphere(edit.Index);

                    EditSystem.PushUndo(EditMode.MeshEditing, new PointEdit
                    {
                        Action = edit.Action,
                        Index = edit.Index,
                        State = current
                    });
                }
                break;

            case EditAction.Add:
                _points.Insert(edit.Index, edit.State);
                if (_editingMode)
                    _debugSpheres.Insert(edit.Index, SpawnDebugSphere(edit.State.Position));

                EditSystem.PushUndo(EditMode.MeshEditing, new PointEdit
                {
                    Action = EditAction.Add,
                    Index = edit.Index,
                    State = edit.State
                });
                break;

            case EditAction.Remove:
                if (edit.Index >= 0 && edit.Index < _points.Count)
                {
                    var removed = _points[edit.Index];
                    _points.RemoveAt(edit.Index);
                    if (_editingMode && edit.Index < _debugSpheres.Count)
                    {
                        _debugSpheres[edit.Index].QueueFree();
                        _debugSpheres.RemoveAt(edit.Index);
                    }

                    EditSystem.PushUndo(EditMode.MeshEditing, new PointEdit
                    {
                        Action = EditAction.Remove,
                        Index = edit.Index,
                        State = removed
                    });
                }
                break;
        }

        GenerateMeshFromPoints();
        GenerateEdgesFromPoints();
    }



    public void UpdateDebugSphere(int index)
    {
        if (!_editingMode || index < 0 || index >= _debugSpheres.Count)
            return;

        var sphere = _debugSpheres[index];

        if (!IsInstanceValid(sphere))
        {
            if (_selectedPointIndex == index)
                DeselectSphere();
            return;
        }

        sphere.GlobalTransform = _points[index].ToTransform();
    }

    public void SelectPoint(int index)
    {
        if (index < 0 || index >= _points.Count || index >= _debugSpheres.Count)
            return;

        DeselectSphere();
        _selectedPointIndex = index;
        AttachGizmoToSelected();
        _dragStartPos = _points[index].Position;
    }
    
    public void OnAddPointClicked()
    {
        Vector3 newPoint = _points.Count > 0 ? _points[^1].Position + new Vector3(0.5f, 0, 0) : Vector3.Zero;
        AddPoint(newPoint);
    }

    public void OnDeletePointClicked()
    {
        DeleteSelectedPoint();
    }

    public void OnClearPointsClicked()
    {
        ClearPoints();
    }

    public void OnToggleEditingMode()
    {
        ClearDebugSpheres();

        if (_editingMode)
        {
            foreach (var cp in _points)
                SpawnDebugSphere(cp.Position);
        }
    }

    
    private bool IsInstanceValid(Node node) => node != null && node.IsInsideTree();
}