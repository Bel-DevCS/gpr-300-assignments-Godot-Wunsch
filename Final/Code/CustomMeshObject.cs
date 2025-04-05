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

    public Stack<PointEdit> _undoStack = new();
    private Stack<PointEdit> _redoStack = new();
    private Vector3 _dragStartPos;
    private const int MAX_HISTORY = 100;
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
        _runtimeUndoBaseline = _undoStack.Count;
        
        _ui = new CustomMeshObject_UI(this);
    }
    

    public override void _Process(double delta)
    {
        if (!_editingInInspector)
            UpdatePointsFromDebugSpheres();

        GenerateMeshFromPoints();
        _ui.DrawEditor();
    }
    private void UpdatePointsFromDebugSpheres()
    {
        for (int i = 0; i < _debugSpheres.Count; i++)
        {
            Transform3D xform = _debugSpheres[i].GlobalTransform;
            if (i >= _points.Count) continue;

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

                    _undoStack.Push(new PointEdit
                    {
                        Action = action,
                        Index = _selectedPointIndex,
                        State = _points[_selectedPointIndex]
                    });

                    _redoStack.Clear();
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
                    case Key.Key1:
                        _gizmo.Mode = Gizmo3D.ToolMode.Move;
                        break;
                    case Key.Key2:
                        _gizmo.Mode = Gizmo3D.ToolMode.Rotate;
                        break;
                    case Key.Key3:
                        _gizmo.Mode = Gizmo3D.ToolMode.Scale;
                        break;
                    case Key.Escape:
                        DeselectSphere();
                        break;
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

        if (_points.Count == 1)
        {
            // Draw a small crosshair or placeholder triangle at the point
            Vector3 p = _points[0].Position;
            float size = _thickness;
            _mesh.SurfaceAddVertex(p + new Vector3(size, 0, 0));
            _mesh.SurfaceAddVertex(p + new Vector3(0, size, 0));
            _mesh.SurfaceAddVertex(p + new Vector3(0, 0, size));
        }
        else if (_points.Count == 2)
        {
            var p0 = _points[0];
            var p1 = _points[1];

            Vector3 dir = (p1.Position - p0.Position).Normalized();
            Vector3 right = dir.Cross(Vector3.Up).Normalized() * _thickness;

            Vector3 a = p0.Position + right;
            Vector3 b = p0.Position - right;
            Vector3 c = p1.Position + right;
            Vector3 d = p1.Position - right;

            _mesh.SurfaceAddVertex(a);
            _mesh.SurfaceAddVertex(b);
            _mesh.SurfaceAddVertex(c);

            _mesh.SurfaceAddVertex(b);
            _mesh.SurfaceAddVertex(d);
            _mesh.SurfaceAddVertex(c);
        }


        else if (_points.Count >= 3)
        {
            for (int i = 0; i < _points.Count - 2; i++)
            {
                Vector3 a = _points[i].Position;
                Vector3 b = _points[i + 1].Position;
                Vector3 c = _points[i + 2].Position;

                _mesh.SurfaceAddVertex(a);
                _mesh.SurfaceAddVertex(b);
                _mesh.SurfaceAddVertex(c);
            }
        }

        _mesh.SurfaceEnd();
    }
    private void AddPoint(Vector3 point)
    {
        var cp = new ControlPoint
        {
            Label = $"Point {_points.Count}",
            Position = point,
            Rotation = Basis.Identity,
            Scale = Vector3.One
        };
        
        _points.Add(cp);
        PushUndo(new PointEdit 
        {
            Action = EditAction.Add,
            Index = _points.Count - 1,
            State = cp
        });

        _redoStack.Clear();
        if (_editingMode) SpawnDebugSphere(point);
    }
    private void DeleteSelectedPoint()
    {
        if (_selectedPointIndex < 0 || _selectedPointIndex >= _points.Count)
            return;

        // Store before deselecting (since it resets _selectedPointIndex)
        int indexToDelete = _selectedPointIndex;
        var deletedPoint = _points[indexToDelete];

        PushUndo(new PointEdit
        {
            Action = EditAction.Remove,
            Index = indexToDelete,
            State = deletedPoint
        });

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
        if (_undoStack.Count <= _runtimeUndoBaseline)
            return; // Don't undo past runtime baseline

        var edit = _undoStack.Pop();
    
        // Save for redo
        if (edit.Action == EditAction.Remove)
        {
            _points.Insert(edit.Index, edit.State);
            _redoStack.Push(new PointEdit { Action = EditAction.Add, Index = edit.Index, State = edit.State });
            if (_editingMode)
                _debugSpheres.Insert(edit.Index, SpawnDebugSphere(edit.State.Position));
        }
        else if (edit.Action == EditAction.Add)
        {
            if (edit.Index >= 0 && edit.Index < _points.Count)
            {
                _redoStack.Push(new PointEdit { Action = EditAction.Remove, Index = edit.Index, State = _points[edit.Index] });
                _points.RemoveAt(edit.Index);
                if (_editingMode && edit.Index < _debugSpheres.Count)
                {
                    _debugSpheres[edit.Index].QueueFree();
                    _debugSpheres.RemoveAt(edit.Index);
                }
            }
        }
        else
        {
            var current = _points[edit.Index];
            _redoStack.Push(new PointEdit { Action = edit.Action, Index = edit.Index, State = current });
            _points[edit.Index] = edit.State;
            UpdateDebugSphere(edit.Index);
        }

        GenerateMeshFromPoints();
    }
    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var edit = _redoStack.Pop();

        if (edit.Action == EditAction.Add)
        {
            _points.Insert(edit.Index, edit.State);
            _undoStack.Push(new PointEdit
            {
                Action = EditAction.Remove,
                Index = edit.Index,
                State = edit.State
            });

            if (_editingMode)
                _debugSpheres.Insert(edit.Index, SpawnDebugSphere(edit.State.Position));
        }
        else if (edit.Action == EditAction.Remove)
        {
            if (edit.Index >= 0 && edit.Index < _points.Count)
            {
                _undoStack.Push(new PointEdit
                {
                    Action = EditAction.Add,
                    Index = edit.Index,
                    State = _points[edit.Index]
                });

                _points.RemoveAt(edit.Index);
                if (_editingMode && edit.Index < _debugSpheres.Count)
                {
                    _debugSpheres[edit.Index].QueueFree();
                    _debugSpheres.RemoveAt(edit.Index);
                }
            }
        }
        else
        {
            if (edit.Index >= 0 && edit.Index < _points.Count)
            {
                var current = _points[edit.Index];
                _undoStack.Push(new PointEdit { Action = edit.Action, Index = edit.Index, State = current });
                _points[edit.Index] = edit.State;
                UpdateDebugSphere(edit.Index);
            }
        }

        GenerateMeshFromPoints();
    }
    public void UpdateDebugSphere(int index)
    {
        if (_editingMode && index >= 0 && index < _debugSpheres.Count)
            _debugSpheres[index].GlobalTransform = _points[index].ToTransform();
    }
    public void PushUndo(PointEdit edit)
    {
        _undoStack.Push(edit);
        if (_undoStack.Count > MAX_HISTORY)
            _undoStack = new Stack<PointEdit>(_undoStack.ToArray()[..MAX_HISTORY]); 
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