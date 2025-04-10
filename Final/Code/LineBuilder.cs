using Godot;
using ImGuiNET;
using System;
using System.Numerics;
using Vector3 = Godot.Vector3; // For ImGui

public class LineBuilder
{
    private Vector3 _startPoint;
    private Vector3 _endPoint;

    public float CurveAmount = 0.0f;
    public int SegmentCount = 16;

    private ImmediateMesh _mesh;

    public LineBuilder()
    {
        _mesh = new ImmediateMesh();
        _startPoint = Vector3.Zero;
        _endPoint = new Vector3(1, 0, 0);
    }

    public void SetEndpoints(Vector3 start, Vector3 end)
    {
        _startPoint = start;
        _endPoint = end;
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

    public void DrawLine()
    {
        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);

        for (int i = 0; i <= SegmentCount; i++)
        {
            float t = i / (float)SegmentCount;
            Vector3 point = GetPointAlongLine(t);
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

    public void DrawImGuiUI()
    {
        ImGui.SliderFloat("Curve Amount", ref CurveAmount, -5f, 5f);
        ImGui.SliderInt("Segments", ref SegmentCount, 2, 64);
    }
}

