using Godot;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vector3 = Godot.Vector3; // For ImGui

public class LineBuilder
{
    private Vector3 _startPoint;
    private Vector3 _endPoint;
    public float CurveAmount = 0.0f;
    public int SegmentCount = 16;
    private ImmediateMesh _mesh;

    // This will hold the points along the curve
    public List<Vector3> Points { get; private set; }

    public LineBuilder()
    {
        _mesh = new ImmediateMesh();
        _startPoint = Vector3.Zero;
        _endPoint = new Vector3(1, 0, 0);
        Points = new List<Vector3>();
    }

    public void SetEndpoints(Vector3 start, Vector3 end)
    {
        _startPoint = start;
        _endPoint = end;
    }

    public void DrawLine()
    {
        Points.Clear();  // Clear previous points

        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);

        for (int i = 0; i <= SegmentCount; i++)
        {
            float t = i / (float)SegmentCount;
            Vector3 point = GetPointAlongLine(t);
            Points.Add(point);  // Store the point for later use
            _mesh.SurfaceAddVertex(point);
        }

        _mesh.SurfaceEnd();
    }

    private Vector3 GetPointAlongLine(float t)
    {
        Vector3 mid = (_startPoint + _endPoint) * 0.5f + new Vector3(0, CurveAmount, 0);
        Vector3 a = _startPoint.Lerp(mid, t);
        Vector3 b = mid.Lerp(_endPoint, t);
        return a.Lerp(b, t);
    }

    public void DrawImGui()
    {
        ImGui.SliderFloat("Curve Amount", ref CurveAmount, -5f, 5f);
        ImGui.SliderInt("Segments", ref SegmentCount, 2, 64);
    }

    public MeshInstance3D CreateMeshInstance()
    {
        var instance = new MeshInstance3D
        {
            Mesh = _mesh,
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = Colors.Yellow,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            }
        };
        return instance;
    }
}


