using Godot;
using ImGuiNET;
using System;
using System.Linq;

public class MeshBuilderUI
{
    private readonly MeshBuilderNode _node;

    private FaceNode _selectedFace;
    private EdgeNode _selectedEdge;

    private int _edgePointAIndex = 0;
    private int _edgePointBIndex = 1;

    private PointNode _currentlyRenaming = null;

    
    public MeshBuilderUI(MeshBuilderNode node)
    {
        _node = node;
    }

    public void Draw()
    {
        DrawPointEditor();
        DrawEdgeEditor();
        DrawFaceEditor();
        DrawMeshActions();
        
        DrawLooping();

    }

   private void DrawPointEditor()
{
    if (!ImGui.Begin("Point Editor")) return;

    ImGui.Text("Points");
    ImGui.Separator();

    ImGui.BeginChild("PointList", new System.Numerics.Vector2(0, 150));
    var points = _node.GetPoints();
    for (int i = 0; i < points.Count; i++)
    {
        var point = points[i];
        bool isSelected = (_node.SelectedPoint == point);

        ImGui.PushID((int)point.GetInstanceId());

        // Renaming mode
        if (_currentlyRenaming == point)
        {
            byte[] buffer = new byte[32];
            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(point.Label);
            Array.Copy(nameBytes, buffer, Math.Min(buffer.Length, nameBytes.Length));

            if (ImGui.InputText("##rename", buffer, (uint)buffer.Length, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                point.Label = System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');
                _currentlyRenaming = null;
            }

            if (!ImGui.IsItemActive() && ImGui.GetIO().MouseClicked[0])
                _currentlyRenaming = null;
        }
        else
        {
            if (ImGui.Selectable(point.Label, isSelected))
            {
                if (ImGui.GetIO().KeyShift)
                    _node.AddToLoopSelection(point);
                else
                    _node.SelectPoint(point);
            }

            // CTRL+click to rename
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(0) && ImGui.GetIO().KeyCtrl)
                _currentlyRenaming = point;
        }

        ImGui.PopID();
    }
    ImGui.EndChild();

    ImGui.Spacing();
    ImGui.Separator();

    if (_node.SelectedPoint != null)
    {
        var selected = _node.SelectedPoint;
        ImGui.Text($"Selected: {selected.Label}");
        var pos = selected.Position;
        var posVec = new System.Numerics.Vector3(pos.X, pos.Y, pos.Z);

        if (ImGui.DragFloat3("Position", ref posVec, 0.05f))
            selected.Position = new Vector3(posVec.X, posVec.Y, posVec.Z);

        if (ImGui.Button("Reset to Origin"))
            selected.Position = Vector3.Zero;
    }
    else
    {
        ImGui.Text("No point selected.");
    }

    ImGui.End();
}

   private void DrawEdgeEditor()
{
    if (!ImGui.Begin("Edge Editor")) return;

    var points = _node.GetPoints();
    var labels = points.Select(p => p.Label).ToArray();

    if (ImGui.CollapsingHeader("Create Edge", ImGuiTreeNodeFlags.DefaultOpen))
    {
        if (labels.Length >= 2)
        {
            ImGui.Combo("Point A", ref _edgePointAIndex, labels, labels.Length);
            ImGui.Combo("Point B", ref _edgePointBIndex, labels, labels.Length);

            if (_edgePointAIndex != _edgePointBIndex)
            {
                if (ImGui.Button("Create Edge"))
                {
                    var a = points[_edgePointAIndex];
                    var b = points[_edgePointBIndex];
                    _node.AddEdge(a, b);
                }
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(1f, 0.5f, 0.5f, 1f), "Cannot link a point to itself.");
            }
        }
        else
        {
            ImGui.Text("Need at least 2 points.");
        }
    }

    ImGui.Spacing();
    ImGui.Separator();

    if (ImGui.CollapsingHeader("Edge List", ImGuiTreeNodeFlags.DefaultOpen))
    {
        var edges = _node.GetEdges();

        ImGui.BeginChild("EdgeList", new System.Numerics.Vector2(0, 150));
        for (int i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            bool isSelected = (_selectedEdge == edge);

            ImGui.PushID((int)edge.GetInstanceId());

            if (ImGui.Selectable($"[{i}] {edge.PointA.Label} --> {edge.PointB.Label}", isSelected))
                _selectedEdge = edge;

            edge.SetHighlighted(isSelected);
            ImGui.PopID();
        }
        ImGui.EndChild();
    }

    ImGui.Spacing();
    ImGui.Separator();

    if (_selectedEdge != null)
    {
        if (ImGui.CollapsingHeader("Selected Edge", ImGuiTreeNodeFlags.DefaultOpen))
        {
            _selectedEdge.Line.DrawImGui();
            _selectedEdge.UpdateEdge();
            foreach (var face in _selectedEdge.ConnectedFaces)
                face.UpdateFace();
        }
    }
    else
    {
        ImGui.TextDisabled("No edge selected.");
    }

    ImGui.End();
}

private void DrawFaceEditor()
{
    if (!ImGui.Begin("Face Editor")) return;

    if (ImGui.CollapsingHeader("Face List", ImGuiTreeNodeFlags.DefaultOpen))
    {
        var faces = _node.GetFaces();

        ImGui.BeginChild("FaceList", new System.Numerics.Vector2(0, 150));
        for (int i = 0; i < faces.Count; i++)
        {
            var face = faces[i];
            string label = $"Face {i}";
            bool isSelected = (_selectedFace == face);

            ImGui.PushID((int)face.GetInstanceId());
            if (ImGui.Selectable(label, isSelected))
                _selectedFace = face;
            ImGui.PopID();
        }
        ImGui.EndChild();
    }

    ImGui.Spacing();
    ImGui.Separator();

    if (_selectedFace != null)
    {
        if (ImGui.CollapsingHeader("Selected Face", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var color = _selectedFace.FaceColor;
            var c = new System.Numerics.Vector4(color.R, color.G, color.B, color.A);

            if (ImGui.ColorEdit4("Face Color", ref c))
                _selectedFace.FaceColor = new Color(c.X, c.Y, c.Z, c.W);

            if (ImGui.Button("Flip Face"))
                _selectedFace.Flip();

            ImGui.SameLine();
            if (ImGui.Button("Clear Selection"))
                _selectedFace = null;
        }
    }
    else
    {
        ImGui.TextDisabled("No face selected.");
    }

    ImGui.End();
}

private void DrawMeshActions()
{
    if (!ImGui.Begin("Mesh Actions")) return;

    ImGui.Text("Quick Tools");
    ImGui.Separator();

    if (ImGui.Button("Add Point"))
    {
        _node.AddPoint(Vector3.Zero);
    }

    ImGui.SameLine();
    if (ImGui.Button("Rebuild Mesh"))
    {
        _node.GenerateMeshFromChildren();
    }

    ImGui.Spacing();
    ImGui.Separator();

    ImGui.Text("Preset Shapes");
    
    if (ImGui.Button("Add Triangle"))
    {
       // _node.ClearMesh();
        _node.GenerateTriangle(Vector3.Zero);
    }

    ImGui.SameLine();
    if (ImGui.Button("Add Square"))
    {
     //   _node.ClearMesh();
        _node.GenerateSquare(Vector3.Zero);
    }

    ImGui.Spacing();
    ImGui.Separator();

    if (ImGui.Button("Clear All"))
    {
        _node.ClearMesh();
    }

    ImGui.End();
}

   private void DrawLooping()
{
    if (!ImGui.Begin("Group Manager")) return;

    var loop = _node.SelectedLoop;

    if (loop.Count == 0)
    {
        ImGui.Text("Shift-click points to begin building a loop.");
    }
    else
    {
        ImGui.Text($"Loop Points: {loop.Count}");
        ImGui.BeginChild("LoopPath", new System.Numerics.Vector2(0, 100));
        for (int i = 0; i < loop.Count; i++)
        {
            var current = loop[i];
            var next = loop[(i + 1) % loop.Count];
            ImGui.Text($"{current.Label} → {next.Label}");
        }
        ImGui.EndChild();

        ImGui.Separator();

        if (ImGui.Button("Create Loop Edges"))
        {
            for (int i = 0; i < loop.Count; i++)
            {
                var a = loop[i];
                var b = loop[(i + 1) % loop.Count];
                _node.AddEdge(a, b);
            }
            _node.ClearLoopSelection();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel Loop"))
        {
            _node.ClearLoopSelection();
        }

        ImGui.Spacing();
        ImGui.Separator();

        if (_node.IsLoopClosed())
        {
            if (ImGui.Button("Add Face from Loop"))
            {
                for (int i = 0; i < loop.Count; i++)
                {
                    var a = loop[i];
                    var b = loop[(i + 1) % loop.Count];
                    _node.AddEdge(a, b);
                }

                var face = new FaceNode();
                var loopSet = loop.ToHashSet();
                foreach (var edge in _node.GetEdges())
                {
                    if (loopSet.Contains(edge.PointA) && loopSet.Contains(edge.PointB))
                        face.AddEdge(edge);
                }

                if (!_node.FaceExists(face))
                {
                    _node.AddFace(face);
                    _node.ClearLoopSelection();
                }
                else
                {
                    ImGui.TextColored(new System.Numerics.Vector4(1, 0.5f, 0.5f, 1), "Face already exists!");
                }
            }
        }
        else
        {
            ImGui.TextColored(new System.Numerics.Vector4(1, 0.5f, 0.5f, 1), "Loop must be closed to generate a face.");
        }

        ImGui.Spacing();
        ImGui.Separator();

        // Placeholder for future group features
        if (ImGui.Button("Create Group (WIP)"))
        {
            ImGui.Text("Grouping logic coming soon...");
        }
    }

    ImGui.End();
}

}
