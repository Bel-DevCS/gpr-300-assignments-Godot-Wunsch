using Godot;
using System;
using System.Collections.Generic;

public partial class EdgeGenerator : Node
{
    private List<LineBuilder> _edges = new();
    private List<MeshInstance3D> _edgeMeshes = new();

    public void GenerateEdgesFromPoints(List<PointGenerator.ControlPoint> points)
    {
        _edgeMeshes.Clear();
        _edges.Clear();

        for (int i = 0; i < points.Count; i++)
        {
            int nextIndex = (i + 1) % points.Count;
            Vector3 start = points[i].Position;
            Vector3 end = points[nextIndex].Position;

            var line = new LineBuilder();
            line.SetEndpoints(start, end);
            line.CurveAmount = 0.0f;
            line.DrawLine();

            var mesh = line.CreateMeshInstance();
            _edgeMeshes.Add(mesh);
            _edges.Add(line);
        }
    }

    public List<LineBuilder> GetEdges() => _edges;
    public List<MeshInstance3D> GetEdgeMeshes() => _edgeMeshes;
}