using Godot;
using System;
using System.Collections.Generic;
using Gizmo3DPlugin;
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
    
    private Godot.Collections.Dictionary<int, MeshInstance3D> _spheres = new();
    private Godot.Collections.Dictionary<int, Vector3> _lastSpherePositions = new();
    
    private int _selectedSphereIndex = -1;

    private Gizmo3D _gizmo = new();
    
    private bool _isPaused = false;

    private bool _isInit = false;

    public override void _Ready()
    {
        if (_path == null)
        {
            GD.PrintErr("Path3D node is not assigned!");
            return;
        }

        _curve.Closed = true;

        ResetCurve();
        AddMovingObject();
        _isInit = true;

        _gizmo = new Gizmo3D();
        AddChild(_gizmo);
        _gizmo.Mode = Gizmo3D.ToolMode.Move;
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

        // Add collision shape so the sphere can be clicked
        StaticBody3D body = new StaticBody3D();
        CollisionShape3D collider = new CollisionShape3D();
        SphereShape3D sphereShape = new SphereShape3D
        {
            Radius = 0.1f
        };

        collider.Shape = sphereShape;
        body.AddChild(collider);
        body.Position = Vector3.Zero; // Keep collision centered in the sphere
        sphere.AddChild(body);

        parent.AddChild(sphere);
        _spheres[index] = sphere;
        _lastSpherePositions[index] = position; // Store initial positions
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
        if (_curve == null || _cube == null || _curve.PointCount == 0)
            return;

        _progress += (float)delta * _speed;

        if (_progress > 1f)
            _progress = 0f;

        // Ensure we do not attempt to sample an empty curve
        if (_curve.PointCount > 0)
        {
            Vector3 newPos = _curve.SampleBaked(_progress * _curve.GetBakedLength(), true);
            _cube.Position = newPos;
        }

        // Sync sphere positions with curve points
        for (int i = 0; i < _curve.PointCount; i++)
        {
            if (_spheres.TryGetValue(i, out MeshInstance3D sphere))
            {
                if (!_lastSpherePositions.ContainsKey(i)) continue; // Avoid missing entries
            
                Vector3 newSpherePos = _curve.GetPointPosition(i);
                if (_lastSpherePositions[i] != newSpherePos)
                {
                    sphere.Position = newSpherePos;
                    _lastSpherePositions[i] = newSpherePos;
                }
            }
        }

        // Ensure only valid spheres are checked for movement
        List<int> toRemove = new List<int>();
        foreach (var kvp in _spheres)
        {
            int index = kvp.Key;
            MeshInstance3D sphere = kvp.Value;

            // Check if the curve still has this index
            if (index >= _curve.PointCount)
            {
                toRemove.Add(index);
                continue;
            }

            if (_lastSpherePositions.ContainsKey(index) && _lastSpherePositions[index] != sphere.Position)
            {
                _curve.SetPointPosition(index, sphere.Position);
                _lastSpherePositions[index] = sphere.Position;
            }
        }

        // Remove any spheres that are no longer valid
        foreach (int index in toRemove)
        {
            _spheres.Remove(index);
            _lastSpherePositions.Remove(index);
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
        if (!_isInit)
        {
            GD.Print("Setting Curve");
        }
        else
        {
            GD.Print("Resetting Curve...");
        }
       

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
    
    
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                HandleSphereSelection(mouseEvent.Position);
            }
        }
        else if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Up) // Add point at the last sphere position + offset
            {
                Vector3 newPoint = _spheres.Count > 0
                    ? _spheres[_spheres.Count - 1].Position + new Vector3(0.5f, 0, 0)
                    : new Vector3(0, 0, 0); // Default starting point

                AddPoint(newPoint);
            }
            else if (keyEvent.Keycode == Key.Down && _selectedSphereIndex != -1) // Remove selected point
            {
                RemovePoint(_selectedSphereIndex);
                _selectedSphereIndex = -1; // Reset selection
            }
        }
    }
    
    private void HandleSphereSelection(Vector2 mousePosition)
    {
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        Vector3 rayOrigin = camera.ProjectRayOrigin(mousePosition);
        Vector3 rayDirection = camera.ProjectRayNormal(mousePosition) * 1000f;

        var spaceState = GetViewport().GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayOrigin + rayDirection);
        var result = spaceState.IntersectRay(query);

        if (result.Count > 0 && result["collider"].Obj is StaticBody3D collider)
        {
            // Ensure we select the correct re-indexed sphere
            foreach (var kvp in _spheres)
            {
                if (kvp.Value.GetChild(0) == collider) // The StaticBody3D is the collider
                {
                    if (_selectedSphereIndex != kvp.Key)
                    {
                        DeselectSphere();
                        _selectedSphereIndex = kvp.Key;
                        AttachGizmoToSelectedSphere();
                        UpdateSphereColors();
                    }
                    return;
                }
            }
        }
    }


    public void AddPoint(Vector3 position)
    {
        int index = _curve.PointCount;
        Vector3 inHandle = Vector3.Zero;
        Vector3 outHandle = Vector3.Zero;

        // If there's at least one point, calculate tangents
        if (index > 0)
        {
            Vector3 prevPoint = _curve.GetPointPosition(index - 1);
            Vector3 direction = (position - prevPoint).Normalized();
        
            // Generate curve handles
            inHandle = direction * -0.2f;
            outHandle = direction * 0.2f;
        }

        _curve.AddPoint(position);
        _curve.SetPointIn(index, inHandle);
        _curve.SetPointOut(index, outHandle);

        AddSphereAtPoint(index, position, _sphereHolder);
        _lastSpherePositions[index] = position;

        DrawCurve();
    }


    public void RemovePoint(int index)
    {
        if (index < 0 || index >= _curve.PointCount)
        {
            GD.PrintErr($"Invalid index {index} for removal.");
            return;
        }

        // **Deselect the gizmo if the selected sphere is being removed**
        if (_selectedSphereIndex == index)
        {
            DeselectSphere(); // Ensures gizmo is fully removed
        }

        // Remove the point from the curve
        _curve.RemovePoint(index);

        // Remove the sphere from the scene
        if (_spheres.TryGetValue(index, out MeshInstance3D sphere))
        {
            _gizmo.Deselect(sphere); // Ensures the gizmo detaches
            sphere.QueueFree();
        }

        // Remove from dictionaries
        _spheres.Remove(index);
        _lastSpherePositions.Remove(index);

        // **Rebuild sphere index mapping to maintain correct order**
        Godot.Collections.Dictionary<int, MeshInstance3D> newSpheres = new();
        Godot.Collections.Dictionary<int, Vector3> newLastPositions = new();

        int newIndex = 0;
        foreach (var kvp in _spheres)
        {
            newSpheres[newIndex] = kvp.Value;
            newLastPositions[newIndex] = _lastSpherePositions[kvp.Key];
            newIndex++;
        }

        _spheres = newSpheres;
        _lastSpherePositions = newLastPositions;

        // **Ensure gizmo is fully removed if no points remain**
        if (_spheres.Count == 0)
        {
            _selectedSphereIndex = -1;
            _gizmo.Deselect(_path);  // Fully remove gizmo selection
        }

        // **Update curve to reflect new sphere order**
        for (int i = 0; i < _curve.PointCount; i++)
        {
            _curve.SetPointPosition(i, _lastSpherePositions[i]);
        }

        DrawCurve();
    }

    private void AttachGizmoToSelectedSphere()
    {
        if (_selectedSphereIndex != -1 && _spheres.TryGetValue(_selectedSphereIndex, out MeshInstance3D sphere))
        {
            _gizmo.GlobalTransform = sphere.GlobalTransform;
            _gizmo.Select(sphere);
        }
        else
        {
            _gizmo.Deselect(_path);
        }
    }


    
    private void DeselectSphere()
    {
        if (_selectedSphereIndex != -1 && _spheres.TryGetValue(_selectedSphereIndex, out MeshInstance3D sphere))
        {
            _gizmo.Deselect(sphere);
        }

        _selectedSphereIndex = -1;
        _gizmo.Deselect(_path);
    }



    
    private void UpdateSphereColors()
    {
        foreach (var kvp in _spheres)
        {
            var sphere = kvp.Value;
            if (sphere == null) continue;

            var material = sphere.MaterialOverride as StandardMaterial3D;
            if (material == null) continue;

            material.AlbedoColor = (kvp.Key == _selectedSphereIndex) ? Colors.Green : Colors.DarkRed;
        }
    }
    
    
    public override void _ExitTree()
    {
        if (_gizmo != null && IsInstanceValid(_gizmo))
        {
            _gizmo.Deselect(_path);
            RemoveChild(_gizmo);
            _gizmo.QueueFree();
            _gizmo = null;
        }
    }

}
