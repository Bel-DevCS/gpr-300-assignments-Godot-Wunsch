using Godot;
using ImGuiNET;
using System;
using System.Numerics;
using Vector3 = Godot.Vector3; // For ImGui

public class LineBuilder
{
    public Vector3 StartPoint;
    public Vector3 EndPoint;
    public float CurveAmount = 0.0f;
    public int SegmentCount = 16;

    private ImmediateMesh _mesh;

    public LineBuilder()
    {
        _mesh = new ImmediateMesh();
        StartPoint = Vector3.Zero;
        EndPoint = new Vector3(1, 0, 0);
    }

    public MeshInstance3D CreateMeshInstance()
    {
        var instance = new MeshInstance3D();
        instance.Mesh = _mesh;
        instance.MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = Colors.Yellow,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
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
        Vector3 mid = (StartPoint + EndPoint) * 0.5f + new Vector3(0, CurveAmount, 0);
        Vector3 a = StartPoint.Lerp(mid, t);
        Vector3 b = mid.Lerp(EndPoint, t);
        return a.Lerp(b, t);
    }

    public void DrawImGuiUI()
    {
        var start = ToNumerics(StartPoint);
        var end = ToNumerics(EndPoint);

        if (ImGui.SliderFloat3("Start", ref start, -10, 10))
            StartPoint = ToGodot(start);

        if (ImGui.SliderFloat3("End", ref end, -10, 10))
            EndPoint = ToGodot(end);

        ImGui.SliderFloat("Curve Amount", ref CurveAmount, -5f, 5f);
        ImGui.SliderInt("Segments", ref SegmentCount, 2, 64);
    }

    private static System.Numerics.Vector3 ToNumerics(Vector3 godotVec)
    {
        return new System.Numerics.Vector3(godotVec.X, godotVec.Y, godotVec.Z);
    }

    private static Vector3 ToGodot(System.Numerics.Vector3 numericsVec)
    {
        return new Vector3(numericsVec.X, numericsVec.Y, numericsVec.Z);
    }
}
