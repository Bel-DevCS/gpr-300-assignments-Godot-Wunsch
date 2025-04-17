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

        if (ImGui.Button("Add Face (All Edges)"))
        {
            var face = new FaceNode();
            foreach (var edge in _node.GetEdges())
                face.AddEdge(edge);
            _node.AddFace(face);
        }

        if (ImGui.Button("Auto-Generate Face (Loop)"))
            _node.AutoGenerateFace();

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
}
