using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;

public class CustomMeshObject_UI
{
    private CustomMeshObject _owner;

    public CustomMeshObject_UI(CustomMeshObject owner)
    {
        _owner = owner;
    }

    public void DrawEditor()
    {
        if (Engine.IsEditorHint()) return;

        if (ImGui.Begin("Custom Mesh Editor"))
        {
            if (ImGui.Button("Add Point"))
                _owner.OnAddPointClicked();

            if (ImGui.Button("Delete Point"))
                _owner.OnDeletePointClicked();

            if (ImGui.Button("Clear Points"))
                _owner.OnClearPointsClicked();

            if (ImGui.Checkbox("Editing Mode", ref _owner._editingMode))
                _owner.OnToggleEditingMode();

            ImGui.Separator();

            if (_owner._editingMode)
            {
                ImGui.Text("Point Order & Labels:");
                DrawPointList();
            }
         
        }
        ImGui.End();

        if(_owner._editingMode)
            DrawInspector();
    }

    private int _dragIndex = -1;

private void DrawPointList()
{
    // Handle pending reorder BEFORE draw
    if (_owner.PendingReorderFrom >= 0 && _owner.PendingReorderTo >= 0)
    {
        var movedPoint = _owner.Points[_owner.PendingReorderFrom];
        var movedSphere = _owner.DebugSpheres[_owner.PendingReorderFrom];

        _owner.Points.RemoveAt(_owner.PendingReorderFrom);
        _owner.DebugSpheres.RemoveAt(_owner.PendingReorderFrom);

        int insertAt = _owner.PendingReorderTo;
        if (_owner.PendingReorderFrom < _owner.PendingReorderTo)
            insertAt--;

        _owner.Points.Insert(insertAt, movedPoint);
        _owner.DebugSpheres.Insert(insertAt, movedSphere);

        _owner.PendingReorderFrom = -1;
        _owner.PendingReorderTo = -1;

        _owner.SelectedPointIndex = -1; // reset selection on reorder
        _owner.GenerateMeshFromPoints();
    }

    if (_owner.SelectedPointIndex >= _owner.Points.Count)
        _owner.SelectedPointIndex = -1;

    for (int i = 0; i < _owner.Points.Count; i++)
    {
        ImGui.PushID(i);
        ImGui.BeginGroup();

        try
        {
            // Drag handle
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0, 0, 0, 0));
            ImGui.Button("≡");
            ImGui.PopStyleColor();

            if (ImGui.BeginDragDropSource())
            {
                ImGui.SetDragDropPayload("POINT_DRAG", IntPtr.Zero, 0);
                _dragIndex = i;
                ImGui.Text($"Moving: {_owner.Points[i].Label ?? $"Point {i}"}");
                ImGui.EndDragDropSource();
            }

            ImGui.SameLine();

            // Selectable point name
            if (ImGui.Selectable(_owner.Points[i].Label ?? $"Point {i}", _owner.SelectedPointIndex == i))
            {
                _owner.SelectPoint(i);
            }

            // Label editing
            string label = _owner.Points[i].Label ?? $"Point {i}";
            byte[] buffer = new byte[64];
            var encoded = Encoding.UTF8.GetBytes(label);
            Array.Copy(encoded, buffer, encoded.Length);

            if (ImGui.InputText($"##label_{i}", buffer, (uint)buffer.Length))
            {
                var cp = _owner.Points[i];
                cp.Label = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                _owner.Points[i] = cp;
            }

            // Drop target
            if (ImGui.BeginDragDropTarget())
            {
                unsafe
                {
                    if (ImGui.AcceptDragDropPayload("POINT_DRAG").NativePtr != null && _dragIndex >= 0 && _dragIndex != i)
                    {
                        _owner.PendingReorderFrom = _dragIndex;
                        _owner.PendingReorderTo = i;
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


   private bool _editingInInspector = false;
private CustomMeshObject.ControlPoint? _preImGuiEditSnapshot = null;

private void DrawInspector()
{
    if (_owner.SelectedPointIndex < 0 || _owner.SelectedPointIndex >= _owner.Points.Count)
        return;

    if (ImGui.Begin("Selected Point"))
    {
        var cp = _owner.Points[_owner.SelectedPointIndex];

        // Start of edit session
        if (_preImGuiEditSnapshot == null && ImGui.IsMouseDown(0))
            _preImGuiEditSnapshot = cp;

        bool changed = false;

        // Label edit
        string currentLabel = cp.Label ?? $"Point {_owner.SelectedPointIndex}";
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
            _owner.UpdateDebugSphere(_owner.SelectedPointIndex);
            changed = true;
        }

        if (changed)
        {
            _owner.Points[_owner.SelectedPointIndex] = cp;
            _editingInInspector = true;
        }

        
        if (_editingInInspector && !ImGui.IsMouseDown(0))
        {
            _editingInInspector = false;

            if (_preImGuiEditSnapshot != null)
            {
                EditSystem.PushUndo(EditMode.MeshEditing, new CustomMeshObject.PointEdit
                {
                    Action = CustomMeshObject.EditAction.Move,
                    Index = _owner.SelectedPointIndex,
                    State = _preImGuiEditSnapshot.Value
                });


                _preImGuiEditSnapshot = null;
            }
        }
    }

    ImGui.End();
}

}