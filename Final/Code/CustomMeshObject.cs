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
    private bool _editingMode = true;
    private List<MeshInstance3D> _debugSpheres = new();
    private Gizmo3DPlugin.Gizmo3D _gizmo = new();
    private int _selectedPointIndex = -1;

    private Stack<PointEdit> _undoStack = new();
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

    
    private struct ControlPoint
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
    }
    

private void DrawPointListImGui()
{
    // Handle pending reorder BEFORE draw
    if (_pendingReorderFrom >= 0 && _pendingReorderTo >= 0)
    {
        var movedPoint = _points[_pendingReorderFrom];
        var movedSphere = _debugSpheres[_pendingReorderFrom];

        _points.RemoveAt(_pendingReorderFrom);
        _debugSpheres.RemoveAt(_pendingReorderFrom);

        int insertAt = _pendingReorderTo;
        if (_pendingReorderFrom < _pendingReorderTo)
            insertAt--;

        _points.Insert(insertAt, movedPoint);
        _debugSpheres.Insert(insertAt, movedSphere);

        _pendingReorderFrom = -1;
        _pendingReorderTo = -1;

        _selectedPointIndex = -1; // reset selection on reorder
        GenerateMeshFromPoints();
    }

    if (_selectedPointIndex >= _points.Count)
        _selectedPointIndex = -1;

    for (int i = 0; i < _points.Count; i++)
    {
        ImGui.PushID(i);
        ImGui.BeginGroup();

        try
        {
            // Drag handle
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0, 0, 0, 0)); // Transparent
            ImGui.Button("≡");
            ImGui.PopStyleColor();

            if (ImGui.BeginDragDropSource())
            {
                ImGui.SetDragDropPayload("POINT_DRAG", IntPtr.Zero, 0);
                _dragIndex = i;
                ImGui.Text($"Moving: {_points[i].Label ?? $"Point {i}"}");
                ImGui.EndDragDropSource();
            }

            ImGui.SameLine();

            // Selectable point name
            if (ImGui.Selectable(_points[i].Label ?? $"Point {i}", _selectedPointIndex == i))
            {
                SelectPoint(i);
            }


            // Label editing
            string label = _points[i].Label ?? $"Point {i}";
            byte[] buffer = new byte[64];
            var encoded = Encoding.UTF8.GetBytes(label);
            Array.Copy(encoded, buffer, encoded.Length);

            if (ImGui.InputText($"##label_{i}", buffer, (uint)buffer.Length))
            {
                var cp = _points[i];
                cp.Label = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                _points[i] = cp;
            }

            // Drop target
            if (ImGui.BeginDragDropTarget())
            {
                unsafe
                {
                    if (ImGui.AcceptDragDropPayload("POINT_DRAG").NativePtr != null && _dragIndex >= 0 && _dragIndex != i)
                    {
                        _pendingReorderFrom = _dragIndex;
                        _pendingReorderTo = i;
                    }
                }
                ImGui.EndDragDropTarget();
            }
        }
        
        finally
        {
            ImGui.EndGroup();
            ImGui.Separator();
            ImGui.PopID();
        }
    }
}

private void DrawPointInspectorImGui()
{
    if (_selectedPointIndex < 0 || _selectedPointIndex >= _points.Count)
        return;

    if (ImGui.Begin("Selected Point"))
    {
        var cp = _points[_selectedPointIndex];

        // Start of edit session: store snapshot
        if (_preImGuiEditSnapshot == null && ImGui.IsMouseDown(0))
            _preImGuiEditSnapshot = cp;

        bool changed = false;

        // Label edit
        string currentLabel = cp.Label ?? $"Point {_selectedPointIndex}";
        byte[] labelBuffer = new byte[64];
        var encoded = Encoding.UTF8.GetBytes(currentLabel);
        Array.Copy(encoded, labelBuffer, encoded.Length);

        if (ImGui.InputText("Label", labelBuffer, (uint)labelBuffer.Length))
        {
            cp.Label = Encoding.UTF8.GetString(labelBuffer).TrimEnd('\0');
            changed = true;
        }

        // Position edit
        var pos = new System.Numerics.Vector3(cp.Position.X, cp.Position.Y, cp.Position.Z);
        if (ImGui.DragFloat3("Position", ref pos, 0.01f))
        {
            cp.Position = new Vector3(pos.X, pos.Y, pos.Z);
            UpdateDebugSphere(_selectedPointIndex);
            changed = true;
        }

        // Apply live update
        if (changed)
        {
            _points[_selectedPointIndex] = cp;
            _editingInInspector = true; // locks out gizmo sync
        }

        // End of editing session: mouse released
        if (_editingInInspector && !ImGui.IsMouseDown(0))
        {
            _editingInInspector = false;

            if (_preImGuiEditSnapshot != null)
            {
                PushUndo(new PointEdit
                {
                    Action = EditAction.Move,
                    Index = _selectedPointIndex,
                    State = _preImGuiEditSnapshot.Value
                });

                _preImGuiEditSnapshot = null;
            }
        }
    }

    ImGui.End();
}


    public override void _Process(double delta)
    {
        if (!_editingInInspector)
            UpdatePointsFromDebugSpheres();

        GenerateMeshFromPoints();
        DrawImGuiEditor();
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

    private void GenerateMeshFromPoints()
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


    private void DrawImGuiEditor()
    {
        if (Engine.IsEditorHint()) return;

        // First window: main editor
        if (ImGui.Begin("Custom Mesh Editor"))
        {
            if (ImGui.Button("Add Point"))
            {
                Vector3 newPoint = _points.Count > 0 ? _points[^1].Position + new Vector3(0.5f, 0, 0) : Vector3.Zero;
                AddPoint(newPoint);
            }

            if (ImGui.Button("Delete Point"))
                DeleteSelectedPoint();

            if (ImGui.Button("Clear Points"))
                ClearPoints();

            ImGui.SliderFloat("Thickness", ref _thickness, 0.01f, 0.5f);

            if (ImGui.Checkbox("Editing Mode", ref _editingMode))
            {
                ClearDebugSpheres();
                if (_editingMode)
                {
                    foreach (var cp in _points)
                        SpawnDebugSphere(cp.Position);
                }
            }

            ImGui.Separator();
            ImGui.Text("Point Order & Labels:");
            DrawPointListImGui();
        }
        ImGui.End();

        // Separate window: Inspector
        DrawPointInspectorImGui();
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



    private void UpdateDebugSphere(int index)
    {
        if (_editingMode && index >= 0 && index < _debugSpheres.Count)
            _debugSpheres[index].GlobalTransform = _points[index].ToTransform();
    }
    
    private void PushUndo(PointEdit edit)
    {
        _undoStack.Push(edit);
        if (_undoStack.Count > MAX_HISTORY)
            _undoStack = new Stack<PointEdit>(_undoStack.ToArray()[..MAX_HISTORY]); 
    }

    private void SelectPoint(int index)
    {
        if (index < 0 || index >= _points.Count || index >= _debugSpheres.Count)
            return;

        DeselectSphere();
        _selectedPointIndex = index;
        AttachGizmoToSelected();
        _dragStartPos = _points[index].Position;
    }


    private bool IsInstanceValid(Node node) => node != null && node.IsInsideTree();
}