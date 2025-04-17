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
        DrawHierarchy();

    }

    private void DrawPointEditor()
    {
        if (!ImGui.Begin("Point Editor")) return;

        var points = _node.GetPoints();
        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            if (ImGui.Selectable(point.Label, _node.SelectedPoint == point))
                _node.SelectPoint(point);
        }

        if (_node.SelectedPoint != null)
        {
            var pos = _node.SelectedPoint.Position;
            var np = new System.Numerics.Vector3(pos.X, pos.Y, pos.Z);
            if (ImGui.DragFloat3("Position", ref np))
                _node.SelectedPoint.Position = new Vector3(np.X, np.Y, np.Z);
        }

        ImGui.End();
    }

    private void DrawEdgeEditor()
    {
        if (!ImGui.Begin("Edge Editor")) return;

        var points = _node.GetPoints();
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
                    _node.AddEdge(a, b);
            }
        }
        else ImGui.Text("Need 2+ points.");

        ImGui.Separator();

        var edges = _node.GetEdges();
        for (int i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            bool selected = (_selectedEdge == edge);
            if (ImGui.Selectable($"Edge {i}: {edge.PointA.Label} → {edge.PointB.Label}", selected))
                _selectedEdge = edge;

            edge.SetHighlighted(selected);
        }

        if (_selectedEdge != null)
        {
            _selectedEdge.Line.DrawImGui();
            _selectedEdge.UpdateEdge();
            foreach (var face in _selectedEdge.ConnectedFaces)
                face.UpdateFace();
        }

        ImGui.End();
    }

    private void DrawFaceEditor()
    {
        if (!ImGui.Begin("Face Editor")) return;
        
        var faces = _node.GetFaces();
        for (int i = 0; i < faces.Count; i++)
        {
            var face = faces[i];
            var label = $"Face {i}";
            if (ImGui.Selectable(label, _selectedFace == face))
                _selectedFace = face;
        }

        if (_selectedFace != null)
        {
            var color = _selectedFace.FaceColor;
            var c = new System.Numerics.Vector4(color.R, color.G, color.B, color.A);
            if (ImGui.ColorEdit4("Face Color", ref c))
                _selectedFace.FaceColor = new Color(c.X, c.Y, c.Z, c.W);

            if (ImGui.Button("Flip Face"))
            {
                _selectedFace.Flip();
            }
        }


        ImGui.End();
    }

    private void DrawMeshActions()
    {
        if (!ImGui.Begin("Mesh Actions")) return;

        if (ImGui.Button("Add Point"))
            _node.AddPoint(Vector3.Zero);

        if (ImGui.Button("Rebuild Mesh"))
            _node.GenerateMeshFromChildren();

        if (ImGui.Button("Add Triangle"))
        {
            _node.ClearMesh();
            _node.GenerateTriangle(Vector3.Zero);
        }

        if (ImGui.Button("Add Square"))
        {
            _node.ClearMesh();
            _node.GenerateSquare(Vector3.Zero);
        }

        ImGui.End();
    }
    
   private void DrawLooping()
{
    if (!ImGui.Begin("Loop Builder")) return;

    var loop = _node.SelectedLoop;
    if (loop.Count == 0)
    {
        ImGui.Text("Shift-click points to add them to loop.");
    }
    else
    {
        for (int i = 0; i < loop.Count; i++)
        {
            var current = loop[i];
            var next = loop[(i + 1) % loop.Count];
            ImGui.Text($"{current.Label} → {next.Label}");
        }

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

        if (ImGui.Button("Cancel Loop"))
        {
            _node.ClearLoopSelection();
        }

        ImGui.Separator();

        // ✅ Show "Add Face" button only if loop is valid
        if (_node.IsLoopClosed())
        {
            if (ImGui.Button("Add Face from Loop"))
            {
                // Step 1: Create or ensure edges between looped points (with auto-close)
                for (int i = 0; i < loop.Count; i++)
                {
                    var a = loop[i];
                    var b = loop[(i + 1) % loop.Count];
                    _node.AddEdge(a, b);
                }

                // Step 2: Create new FaceNode
                var face = new FaceNode();

                // Step 3: Add edges that match loop
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
                    ImGui.Text("Face already exists!");
                }
            }
        }
        else
        {
            ImGui.Text("Loop is not closed.");
        }
    }

    ImGui.End();
}
   
private void DrawHierarchy()
{
    if (!ImGui.Begin("Hierarchy")) return;

    ImGui.Text("Points:");
    ImGui.Separator();

    var points = _node.GetPoints();
    foreach (var point in points)
    {
        bool isSelected = (_node.SelectedPoint == point);
        ImGui.PushID((int)point.GetInstanceId());

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

            // Only cancel if user clicks away
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

            // CTRL + click to rename
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(0) && ImGui.GetIO().KeyCtrl)
                _currentlyRenaming = point;
        }

        ImGui.PopID();
    }

    ImGui.End();
}


}
