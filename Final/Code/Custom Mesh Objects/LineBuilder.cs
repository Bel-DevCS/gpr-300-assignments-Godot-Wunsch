    using Godot;
    using ImGuiNET;
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Vector3 = Godot.Vector3; // For ImGui

    public enum CurveMode
    {
        Linear,
        Quadratic,
        Cubic,
        SineWave,
        Arc,
        Sawtooth,
        ZigZag
    }


public class LineBuilder
{
    private Vector3 _startPoint;
    private Vector3 _endPoint;
    private ImmediateMesh _mesh;

    public CurveMode Mode = CurveMode.Quadratic;
    public int SegmentCount = 16;

    public float CurveAmount = 0.0f; // General usage

    // Quadratic/Cubic
    public Vector3 Bias = new Vector3(0, 1, 0);

    // SineWave
    public float SineFrequency = 2f;
    public float SinePhase = 0f;

    // Spiral
    public int SpiralLoops = 2;
    public float SpiralTaper = 1f;

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
        Points.Clear();
        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.LineStrip);

        for (int i = 0; i <= SegmentCount; i++)
        {
            float t = i / (float)SegmentCount;
            Vector3 point = GetPointAlongLine(t);
            Points.Add(point);
            _mesh.SurfaceAddVertex(point);
        }

        _mesh.SurfaceEnd();
    }

   private Vector3 GetPointAlongLine(float t)
{
    switch (Mode)
    {
        case CurveMode.Linear:
            return _startPoint.Lerp(_endPoint, t);

        case CurveMode.Quadratic:
        {
            Vector3 mid = (_startPoint + _endPoint) * 0.5f + Bias * CurveAmount;
            Vector3 a = _startPoint.Lerp(mid, t);
            Vector3 b = mid.Lerp(_endPoint, t);
            return a.Lerp(b, t);
        }

        case CurveMode.Cubic:
        {
            Vector3 p1 = _startPoint;
            Vector3 p2 = _startPoint + Bias * CurveAmount;
            Vector3 p3 = _endPoint - Bias * CurveAmount;
            Vector3 p4 = _endPoint;

            return Mathf.Pow(1 - t, 3) * p1 +
                   3 * Mathf.Pow(1 - t, 2) * t * p2 +
                   3 * (1 - t) * Mathf.Pow(t, 2) * p3 +
                   Mathf.Pow(t, 3) * p4;
        }

        case CurveMode.SineWave:
        {
            Vector3 dir = _endPoint - _startPoint;
            Vector3 basePos = _startPoint + dir * t;
            float wave = Mathf.Sin(t * SineFrequency * Mathf.Pi * 2 + SinePhase) * CurveAmount;
            return basePos + Bias.Normalized() * wave;
        }

        case CurveMode.Arc:
        {
            // Half-circle arc (bias is up)
            Vector3 dir = _endPoint - _startPoint;
            Vector3 center = (_startPoint + _endPoint) * 0.5f;
            float angle = Mathf.Pi * (t - 0.5f); // Range [-π/2, π/2]
            Vector3 perp = Bias.Normalized() * CurveAmount;
            Vector3 pos = center + Mathf.Cos(angle) * (dir * 0.5f) + Mathf.Sin(angle) * perp;
            return pos;
        }

        case CurveMode.Sawtooth:
        {
            Vector3 dir = _endPoint - _startPoint;
            Vector3 basePos = _startPoint + dir * t;
            float wave = ((t * SineFrequency) % 1.0f) * 2f - 1f;
            return basePos + Bias.Normalized() * wave * CurveAmount;
        }

        case CurveMode.ZigZag:
        {
            Vector3 dir = _endPoint - _startPoint;
            Vector3 basePos = _startPoint + dir * t;
            float wave = Mathf.Sign(MathF.Sin(t * SineFrequency * Mathf.Pi * 2)) * CurveAmount;
            return basePos + Bias.Normalized() * wave;
        }

        default:
            return _startPoint.Lerp(_endPoint, t);
    }
}

public void DrawImGui()
{
    ImGui.SliderInt("Segments", ref SegmentCount, 2, 64);

    var modes = Enum.GetNames(typeof(CurveMode));
    int selected = (int)Mode;
    if (ImGui.Combo("Curve Mode", ref selected, modes, modes.Length))
        Mode = (CurveMode)selected;

    ImGui.SliderFloat("Curve Amount", ref CurveAmount, -5f, 5f);

    switch (Mode)
    {
        case CurveMode.Quadratic:
        case CurveMode.Cubic:
        case CurveMode.Arc:
        case CurveMode.SineWave:
        case CurveMode.Sawtooth:
        case CurveMode.ZigZag:
            ImGui.Text("Bias (direction of curve):");
            System.Numerics.Vector3 biasVec = new(Bias.X, Bias.Y, Bias.Z);
            if (ImGui.DragFloat3("Bias", ref biasVec))
                Bias = new Vector3(biasVec.X, biasVec.Y, biasVec.Z);
            break;
    }

    if (Mode == CurveMode.SineWave || Mode == CurveMode.Sawtooth || Mode == CurveMode.ZigZag)
    {
        ImGui.SliderFloat("Sine Frequency", ref SineFrequency, 0.1f, 10f);
        if (Mode == CurveMode.SineWave)
            ImGui.SliderFloat("Sine Phase", ref SinePhase, 0f, Mathf.Pi * 2);
    }
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

        var body = new StaticBody3D();

        for (int i = 0; i < Points.Count - 1; i++)
        {
            var a = Points[i];
            var b = Points[i + 1];
            var mid = (a + b) * 0.5f;
            var dir = b - a;
            var length = dir.Length();

            var capsule = new CollisionShape3D
            {
                Shape = new CapsuleShape3D
                {
                    Radius = 0.05f,
                    Height = MathF.Max(0.01f, length - 0.1f)
                },
                Transform = new Transform3D(Basis.LookingAt(dir.Normalized(), Vector3.Up), mid)
            };

            body.AddChild(capsule);
        }

        instance.AddChild(body);
        return instance;
    }
}


