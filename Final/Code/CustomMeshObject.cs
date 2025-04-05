using Godot;
using System;
using System.Collections.Generic;

public partial class CustomMeshObject : Node
{
    // Mesh display
    private ImmediateMesh _mesh = new ImmediateMesh();
    private MeshInstance3D _meshInstance;

    // Curve control
    private Curve3D _curve = new Curve3D();
    private List<Vector3> _points = new List<Vector3>();

    // Settings
    private float _thickness = 0.1f;
    
    private bool _editingMode = true;
    private List<MeshInstance3D> _debugSpheres = new();


    public override void _Ready()
    {
        _meshInstance = new MeshInstance3D();
        _meshInstance.Mesh = _mesh;

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = Colors.Blue;
        mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        mat.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        _meshInstance.MaterialOverride = mat;

        AddChild(_meshInstance);
        
        AddPoint(new Vector3(0, 0, 0));
        AddPoint(new Vector3(1, 0, 0));
        AddPoint(new Vector3(2, 0, 1));
    }


    public override void _Process(double delta)
    {
        GenerateMeshFromCurve();
        DrawImGuiEditor();
    }

    public void AddPoint(Vector3 point)
    {
        _points.Add(point);
        _curve.AddPoint(point);

        if (_editingMode)
            SpawnDebugSphere(point);
    }


    public void ClearPoints()
    {
        _points.Clear();
        _curve.ClearPoints();
        ClearDebugSpheres();
    }


    private void GenerateMeshFromCurve()
    {
        if (_curve.PointCount < 2)
            return;

        _mesh.ClearSurfaces();
        _mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

        for (int i = 0; i < _curve.PointCount - 1; i++)
        {
            Vector3 p0 = _curve.GetPointPosition(i);
            Vector3 p1 = _curve.GetPointPosition(i + 1);
            Vector3 dir = (p1 - p0).Normalized();
            Vector3 right = dir.Cross(Vector3.Up).Normalized() * _thickness;

            Vector3 v0 = p0 + right;
            Vector3 v1 = p0 - right;
            Vector3 v2 = p1 + right;
            Vector3 v3 = p1 - right;

            _mesh.SurfaceAddVertex(v0);
            _mesh.SurfaceAddVertex(v1);
            _mesh.SurfaceAddVertex(v2);

            _mesh.SurfaceAddVertex(v1);
            _mesh.SurfaceAddVertex(v3);
            _mesh.SurfaceAddVertex(v2);
        }

        _mesh.SurfaceEnd();

        // Force assignment (ImmediateMesh sometimes needs this)
        _meshInstance.Mesh = _mesh;
    }



    public void DrawImGuiEditor()
    {
        if (Engine.IsEditorHint()) return;

        if (ImGuiNET.ImGui.Begin("Custom Mesh Editor"))
        {
            if (ImGuiNET.ImGui.Button("Add Point"))
            {
                Vector3 newPoint = _points.Count > 0 ? _points[_points.Count - 1] + new Vector3(0.5f, 0, 0) : Vector3.Zero;
                AddPoint(newPoint);
            }

            if (ImGuiNET.ImGui.Button("Clear Points"))
            {
                ClearPoints();
            }

            ImGuiNET.ImGui.SliderFloat("Thickness", ref _thickness, 0.01f, 0.5f);

            if (ImGuiNET.ImGui.Checkbox("Editing Mode", ref _editingMode))
            {
                ClearDebugSpheres();

                if (_editingMode)
                {
                    foreach (var point in _points)
                        SpawnDebugSphere(point);
                }
            }
        }

        ImGuiNET.ImGui.End();
    }

    
    
    private void SpawnDebugSphere(Vector3 position)
    {
        var sphere = new MeshInstance3D();
        sphere.Mesh = new SphereMesh
        {
            Radius = 0.1f,
            Height = 0.2f,
            RadialSegments = 8,
            Rings = 8,
        };

        var mat = new StandardMaterial3D
        {
            AlbedoColor = Colors.DarkRed,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };
        sphere.MaterialOverride = mat;
        sphere.Position = position;

        AddChild(sphere);
        _debugSpheres.Add(sphere);
    }

    private void ClearDebugSpheres()
    {
        foreach (var sphere in _debugSpheres)
        {
            if (IsInstanceValid(sphere))
                sphere.QueueFree();
        }
        _debugSpheres.Clear();
    }
    
}
