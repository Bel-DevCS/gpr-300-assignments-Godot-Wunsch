using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Gizmo3DPlugin;

public partial class CustomMeshObject : Node
{
    private ImmediateMesh _mesh = new ImmediateMesh();
    private MeshInstance3D _meshInstance;

    private Curve3D _curve = new Curve3D();
    private List<Vector3> _points = new List<Vector3>();

    private float _thickness = 0.1f;
    
    private bool _editingMode = true;
    private List<MeshInstance3D> _debugSpheres = new();
    
    private Gizmo3DPlugin.Gizmo3D _gizmo = new();
    private int _selectedPointIndex = -1;
    
    private Stack<PointEdit> _undoStack = new();
    private Stack<PointEdit> _redoStack = new();
    
    private Vector3 _dragStartPos;

    
    private enum EditAction
    {
        Move,
        Rotate,
        Scale,
        Add,
        Remove,
        Clear
    }

    private struct PointEdit
    {
        public EditAction Action;
        public int Index;
        public Vector3 Position;
    }
    

    public override void _Ready()
    {
        _meshInstance = new MeshInstance3D();
        _meshInstance.Mesh = _mesh;

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = Colors.Blue;
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mat.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        _meshInstance.MaterialOverride = mat;

        AddChild(_meshInstance);

        AddPoint(new Vector3(0, 0, 0));
        AddPoint(new Vector3(1, 0, 0));
        AddPoint(new Vector3(2, 0, 1));

        for (int i = 0; i < _points.Count; i++)
        {
            _undoStack.Push(new PointEdit
            {
                Action = EditAction.Move,
                Index = i,
                Position = _points[i]
            });
        }


        AddChild(_gizmo);
        _gizmo.Mode = Gizmo3D.ToolMode.Move;
    }

    public override void _Process(double delta)
    {
        for (int i = 0; i < _debugSpheres.Count; i++)
        {
            Vector3 currentPos = _debugSpheres[i].GlobalTransform.Origin;
            _points[i] = currentPos;
            _curve.SetPointPosition(i, currentPos);
        }
        

        GenerateMeshFromCurve();
        DrawImGuiEditor();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_editingMode)
            return;

        // Mouse selection
        if (@event is InputEventMouseButton mouseBtn)
        {
            // Left Click Pressed → Try selecting a sphere
            if (mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
            {
                TrySelectSphere(mouseBtn.Position);
            }

            // Left Click Released → Check if moved and commit undo
            else if (!mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left && _selectedPointIndex != -1)
            {
                Vector3 newPos = _points[_selectedPointIndex];

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
                        Position = _dragStartPos
                    });

                    _redoStack.Clear();
                    _dragStartPos = newPos;
                }
            }



        }

        // Keyboard shortcuts
        if (@event is InputEventKey key && key.Pressed)
        {
            bool ctrl = Input.IsKeyPressed(Key.Ctrl);

            if (ctrl && key.Keycode == Key.Z)
            {
                Undo();
            }
            else if (ctrl && key.Keycode == Key.Y)
            {
                Redo();
            }
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


    public void AddPoint(Vector3 point)
    {
        _points.Add(point);
        _curve.AddPoint(point);
        _undoStack.Push(new PointEdit { Action = EditAction.Add, Index = _points.Count - 1 });
        _redoStack.Clear();

        if (_editingMode)
            SpawnDebugSphere(point);
    }


    public void ClearPoints()
    {
        for (int i = 0; i < _points.Count; i++)
        {
            _undoStack.Push(new PointEdit { Action = EditAction.Remove, Index = i, Position = _points[i] });
        }

        _redoStack.Clear();
        _points.Clear();
        _curve.ClearPoints();
        ClearDebugSpheres();
    }


    private void GenerateMeshFromCurve()
    {
        if (_curve.PointCount < 2)
            return;

        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

        for (int i = 0; i < _curve.PointCount - 1; i++)
        {
            Vector3 p0 = _curve.GetPointPosition(i);
            Vector3 p1 = _curve.GetPointPosition(i + 1);
            Vector3 dir = (p1 - p0).Normalized();
            Vector3 right = dir.Cross(Vector3.Up).Normalized() * _thickness;

            Vector3 v0 = p0 + right;
            Vector3 v1 = p0 - right;
            Vector3 v2 = p1 + right;
            Vector3 v3 = p1 - right;

            _mesh.SurfaceAddVertex(v0);
            _mesh.SurfaceAddVertex(v1);
            _mesh.SurfaceAddVertex(v2);

            _mesh.SurfaceAddVertex(v1);
            _mesh.SurfaceAddVertex(v3);
            _mesh.SurfaceAddVertex(v2);
        }

        _mesh.SurfaceEnd();
        _meshInstance.Mesh = _mesh;
    }

    public void DrawImGuiEditor()
    {
        if (Engine.IsEditorHint()) return;

        if (ImGuiNET.ImGui.Begin("Custom Mesh Editor"))
        {
            if (ImGuiNET.ImGui.Button("Add Point"))
            {
                Vector3 newPoint = _points.Count > 0 ? _points[^1] + new Vector3(0.5f, 0, 0) : Vector3.Zero;
                AddPoint(newPoint);
            }

            if (ImGuiNET.ImGui.Button("Clear Points"))
            {
                ClearPoints();
            }

            ImGuiNET.ImGui.SliderFloat("Thickness", ref _thickness, 0.01f, 0.5f);

            if (ImGuiNET.ImGui.Checkbox("Editing Mode", ref _editingMode))
            {
                ClearDebugSpheres();
                if (_editingMode)
                {
                    foreach (var point in _points)
                        SpawnDebugSphere(point);
                }
            }
        }
        ImGuiNET.ImGui.End();
    }

    private void SpawnDebugSphere(Vector3 position)
    {
        var sphere = new MeshInstance3D
        {
            Mesh = new SphereMesh
            {
                Radius = 0.1f,
                Height = 0.2f,
                RadialSegments = 8,
                Rings = 8,
            },
            Position = position
        };

        var mat = new StandardMaterial3D
        {
            AlbedoColor = Colors.DarkRed,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };
        sphere.MaterialOverride = mat;

        var body = new StaticBody3D();
        var collider = new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 0.1f }
        };
        body.AddChild(collider);
        body.Position = Vector3.Zero;
        sphere.AddChild(body);

        AddChild(sphere);
        _debugSpheres.Add(sphere);
    }

    private void ClearDebugSpheres()
    {
        foreach (var sphere in _debugSpheres)
        {
            if (IsInstanceValid(sphere))
                sphere.QueueFree();
        }
        _debugSpheres.Clear();
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
                _dragStartPos = _points[_selectedPointIndex];
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

    public void Undo()
    {
        if (_undoStack.Count == 0)
            return;

        var edit = _undoStack.Pop();
    
        if (!IsValidEdit(edit)) return;

        var inverse = GetInverseEdit(edit);
        _redoStack.Push(inverse);

        ApplyEdit(edit, undo: true);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
            return;

        var edit = _redoStack.Pop();

        if (!IsValidEdit(edit)) return;

        var inverse = GetInverseEdit(edit);
        _undoStack.Push(inverse);

        ApplyEdit(edit, undo: false);
    }


    private PointEdit GetInverseEdit(PointEdit edit)
    {
        if (edit.Action == EditAction.Move || edit.Action == EditAction.Rotate || edit.Action == EditAction.Scale)
        {
            if (edit.Index < 0 || edit.Index >= _points.Count)
            {
                GD.PrintErr($"[GetInverseEdit] Invalid index {edit.Index}");
                return edit;
            }

            return new PointEdit
            {
                Action = edit.Action, // keep same action type
                Index = edit.Index,
                Position = _points[edit.Index] // current position becomes "redo"
            };
        }

        return edit.Action switch
        {
            EditAction.Add => new PointEdit
            {
                Action = EditAction.Remove,
                Index = edit.Index,
                Position = _points[Math.Min(edit.Index, _points.Count - 1)]
            },
            EditAction.Remove => new PointEdit
            {
                Action = EditAction.Add,
                Index = edit.Index,
                Position = edit.Position
            },
            _ => edit
        };
    }




    private void ApplyPosition(int index, Vector3 pos)
    {
        if (index < 0 || index >= _points.Count)
        {
            GD.PrintErr($"[ApplyPosition] Invalid index {index}.");
            return;
        }

        _points[index] = pos;
        _curve.SetPointPosition(index, pos);

        if (_editingMode && index < _debugSpheres.Count)
            _debugSpheres[index].GlobalTransform = new Transform3D(Basis.Identity, pos);
    }


    private void InsertPoint(int index, Vector3 pos)
    {
        if (index < 0 || index > _points.Count)
        {
            GD.PrintErr($"[InsertPoint] Invalid index {index} for inserting.");
            return;
        }

        _points.Insert(index, pos);

        _curve.ClearPoints();
        foreach (var p in _points)
            _curve.AddPoint(p);

        if (_editingMode)
        {
            ClearDebugSpheres();
            foreach (var p in _points)
                SpawnDebugSphere(p);
        }
    }

    private bool IsValidEdit(PointEdit edit)
    {
        switch (edit.Action)
        {
            case EditAction.Move:
            case EditAction.Remove:
            case EditAction.Add:
                return edit.Index >= 0 && edit.Index <= _points.Count;

            default:
                return false;
        }
    }

    private void ApplyEdit(PointEdit edit, bool undo)
    {
        switch (edit.Action)
        {
            case EditAction.Move:
            case EditAction.Rotate:
            case EditAction.Scale:
                ApplyPosition(edit.Index, edit.Position);
                break;

            case EditAction.Add:
                if (undo) RemovePoint(edit.Index);
                else InsertPoint(edit.Index, edit.Position);
                break;

            case EditAction.Remove:
                if (undo) InsertPoint(edit.Index, edit.Position);
                else RemovePoint(edit.Index);
                break;
        }

        // Refresh selection visuals
        if (_selectedPointIndex == edit.Index)
        {
            DeselectSphere();
            _selectedPointIndex = edit.Index;
            AttachGizmoToSelected();
        }
    }



    private void RemovePoint(int index)
    {
        if (index < 0 || index >= _points.Count)
        {
            GD.PrintErr($"[RemovePoint] Invalid index {index} for points list.");
            return;
        }

        _points.RemoveAt(index);
        _curve.RemovePoint(index);

        if (_editingMode && index < _debugSpheres.Count)
        {
            var sphere = _debugSpheres[index];
            _debugSpheres.RemoveAt(index);
            sphere.QueueFree();
        }
    }

}
