using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

public partial class MeshBuilderNode : Node3D
{
    private ArrayMesh _mesh = new ArrayMesh();
    private MeshInstance3D _meshInstance;
    
    private List<EdgeNode> _edges = new();
    private List<FaceNode> _faces = new();

    private int _edgePointAIndex = 0;
    private int _edgePointBIndex = 1;

    public override void _Ready()
    {
        _meshInstance = GetNode<MeshInstance3D>("LiveMesh"); // reference existing node
        _meshInstance.Mesh = _mesh;
        GenerateMeshFromChildren();
    }

    public override void _Process(double delta)
    {
        DrawEditor();
    }

    private PointNode _selectedPoint = null;

    private void DrawEditor()
    {
        DrawPointEditor();
        DrawEdgeEditor();
        DrawFaceEditor();
        DrawMeshActions();
    }

    private void DrawPointEditor()
    {
        if (!ImGui.Begin("Point Editor"))
            return;

        // Selection UI
        foreach (var child in GetChildren())
        {
            if (child is PointNode point)
            {
                if (ImGui.Selectable(point.Label, _selectedPoint == point))
                    _selectedPoint = point;
            }
        }

        // Position Editing
        if (_selectedPoint != null)
        {
            Godot.Vector3 godotPos = _selectedPoint.Position;
            System.Numerics.Vector3 numericsPos = new(godotPos.X, godotPos.Y, godotPos.Z);

            if (ImGui.DragFloat3("Position", ref numericsPos))
            {
                _selectedPoint.Position = new Godot.Vector3(numericsPos.X, numericsPos.Y, numericsPos.Z);
            }
        }

        ImGui.End();
    }

    private void DrawEdgeEditor()
    {
        if (!ImGui.Begin("Edge Editor"))
            return;

        var pointList = GetChildren().OfType<PointNode>().ToList();
        string[] pointLabels = pointList.Select(p => p.Label).ToArray();

        if (pointLabels.Length >= 2)
        {
            ImGui.Combo("Point A", ref _edgePointAIndex, pointLabels, pointLabels.Length);
            ImGui.Combo("Point B", ref _edgePointBIndex, pointLabels, pointLabels.Length);

            if (ImGui.Button("Link Points"))
            {
                var a = pointList[_edgePointAIndex];
                var b = pointList[_edgePointBIndex];
                if (a != b)
                    AddEdge(a, b);
            }
        }
        else
        {
            ImGui.Text("At least 2 points needed.");
        }

        ImGui.End();
    }

    private void DrawFaceEditor()
    {
        if (!ImGui.Begin("Face Editor"))
            return;

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
        if (!ImGui.Begin("Mesh Actions"))
            return;

        if (ImGui.Button("Add Point"))
            AddPoint(Vector3.Zero);

        if (ImGui.Button("Rebuild Mesh"))
            GenerateMeshFromChildren();

        ImGui.End();
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

    public void GenerateMeshFromChildren()
    {
        var points = new List<Vector3>();
        foreach (var child in GetChildren())
        {
            if (child is PointNode pointNode)
                points.Add(pointNode.GlobalPosition);
        }

        _mesh.ClearSurfaces();
        if (points.Count < 3) return;

        SurfaceTool st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        Vector3 center = Vector3.Zero;
        foreach (var pt in points)
            center += pt;
        center /= points.Count;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[(i + 1) % points.Count];
            st.AddVertex(center);
            st.AddVertex(a);
            st.AddVertex(b);
        }

        st.GenerateNormals();
        st.Commit(_mesh);
    }
    
    public void AddEdge(PointNode a, PointNode b)
    {
        var edge = new EdgeNode();
        edge.SetPoints(a, b);
        AddChild(edge);
        _edges.Add(edge);
    }
    
    public void AutoGenerateFace()
    {
        if (_edges.Count < 3)
        {
            GD.Print("Need at least 3 edges for a face.");
            return;
        }

        // Naive loop detection — just chains if B of one equals A of next
        var orderedEdges = new List<EdgeNode>();
        var remaining = new List<EdgeNode>(_edges);

        EdgeNode start = remaining[0];
        orderedEdges.Add(start);
        remaining.RemoveAt(0);

        PointNode endPoint = start.PointB;

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
            GD.Print("Auto-generated face with " + orderedEdges.Count + " edges.");
        }
        else
        {
            GD.Print("Edges do not form a closed loop.");
        }
    }


}
