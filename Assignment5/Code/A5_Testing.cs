using Godot;
using System;
using Godot.Collections;
using Godot.NativeInterop;

[Tool]
public partial class A5_Testing : Node
{
    Curve3D _curve = new Curve3D();

    [Export] private Path3D _path;
    [Export] private MeshInstance3D _meshInstance;
    [Export] private Node3D _sphereHolder;
    
    private PathFollow3D _pathFollow;
    private MeshInstance3D _cube;
    
    private float _progress = 0f;
    private float _speed = 0.1f;
    
    private Dictionary<int, MeshInstance3D> _spheres = new();
    
    private bool _isPaused = false;

    public override void _Ready()
    {
        if (_path == null)
        {
            GD.PrintErr("Path3D node is not assigned!");
            return;
        }
        
        _curve.BakeInterval = 0.05f; 

        // Adding points correctly
        AddCurvePoint(new Vector3(0, 0, 0), new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f));
        AddCurvePoint(new Vector3(0, 0, 1), new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f));
        AddCurvePoint(new Vector3(1, 0, 2), new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f));
        AddCurvePoint(new Vector3(1, 0, 1), new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f));
        AddCurvePoint(new Vector3(1, 0, 0), new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f));

        // Close the curve by adding the first point at the end
        Vector3 firstPos = _curve.GetPointPosition(0);
        Vector3 firstIn = _curve.GetPointIn(0);
        Vector3 firstOut = _curve.GetPointOut(0);
        AddCurvePoint(firstPos, firstOut, firstIn); 

        // Assigning the curve to the path
        _path.Curve = _curve;
        _path.Visible = true;
        
        
        for (int i = 0; i < _curve.PointCount; i++)
        {
            Vector3 point = _curve.GetPointPosition(i);
            AddSphereAtPoint(i, _curve.GetPointPosition(i), _sphereHolder);
        }

        AddMovingObject();
    }
    
    private void AddSphereAtPoint(int index, Vector3 position, Node3D parent)
    {
        MeshInstance3D sphere = new MeshInstance3D();
        sphere.Mesh = new SphereMesh()
        {
            Radius = 0.1f,
            Height = 0.2f
        };
        sphere.Position = position;

        StandardMaterial3D sphereMat = new StandardMaterial3D();
        sphereMat.AlbedoColor = Colors.DarkRed;
        
        sphere.MaterialOverride = sphereMat;
        parent.AddChild(sphere);
        
        _spheres[index] = sphere;
    }
    
    private void AddCurvePoint(Vector3 position, Vector3 inHandle, Vector3 outHandle)
    {
        _curve.AddPoint(position);
        int index = _curve.PointCount - 1;
        _curve.SetPointIn(index, inHandle);
        _curve.SetPointOut(index, outHandle);
    }
    
    private void DrawCurve()
    {
        ImmediateMesh immediateMesh = new ImmediateMesh();
        immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

        float length = _curve.GetBakedLength();
        int steps = 300; // Higher value = smoother curve
        for (int i = 0; i < steps; i++)
        {
            float t1 = (i / (float)steps) * length;
            float t2 = ((i + 1) / (float)steps) * length;
            Vector3 start = _curve.SampleBaked(t1);
            Vector3 end = _curve.SampleBaked(t2);
            immediateMesh.SurfaceAddVertex(start);
            immediateMesh.SurfaceAddVertex(end);
        }

        immediateMesh.SurfaceEnd();
        _meshInstance.Mesh = immediateMesh;
    }

    private void AddMovingObject()
    {
        _cube = new MeshInstance3D();
        _cube.Mesh = new BoxMesh()
        {
            Size = new Vector3(0.2f, 0.2f, 0.2f)
        };
        
        StandardMaterial3D cubeMat = new StandardMaterial3D();
        cubeMat.AlbedoColor = Colors.Cyan;
        
        _cube.MaterialOverride = cubeMat;
        
        AddChild(_cube);
    }
    
    public override void _Process(double delta)
    {
        if (_curve == null || _cube == null)
            return;
        
        _progress += (float)delta * _speed;
        
        if (_progress > 1f)
            _progress = 0f;
        
        Vector3 newPos = _curve.SampleBaked(_progress * _curve.GetBakedLength(), true);
        
        _cube.Position = newPos;
        
        for (int i = 0; i < _curve.PointCount; i++)
        {
            if (_spheres.TryGetValue(i, out MeshInstance3D sphere))
            {
                sphere.Position = _curve.GetPointPosition(i);
            }
        }

        
        DrawCurve();
    }
    public void TogglePause()
    {
        _isPaused = !_isPaused;
        GD.Print(_isPaused ? "Paused" : "Resumed");
    }
    
    public void ResetCurve()
    {
        GD.Print("Resetting Curve...");

        // Clear existing curve
        _curve.ClearPoints();

        // Remove all spheres
        foreach (var sphere in _spheres.Values)
        {
            sphere.QueueFree();
        }
        _spheres.Clear();

        // Add new points with tangents
        AddCurvePoint(new Vector3(0, 0, 0), new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f));
        AddCurvePoint(new Vector3(0, 0, 1), new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f));
        AddCurvePoint(new Vector3(1, 0, 2), new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f));
        AddCurvePoint(new Vector3(1, 0, 1), new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f));
        AddCurvePoint(new Vector3(1, 0, 0), new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f));

        // Close the curve by adding the first point at the end
        Vector3 firstPos = _curve.GetPointPosition(0);
        Vector3 firstIn = _curve.GetPointIn(0);
        Vector3 firstOut = _curve.GetPointOut(0);
        AddCurvePoint(firstPos, firstOut, firstIn);

        // Assign to path again
        _path.Curve = _curve;
        
        // Re-add spheres
        for (int i = 0; i < _curve.PointCount; i++)
        {
            AddSphereAtPoint(i, _curve.GetPointPosition(i), _sphereHolder);
        }

        // Force Redraw
        DrawCurve();
        GD.Print("Curve Reset!");
    }
}
