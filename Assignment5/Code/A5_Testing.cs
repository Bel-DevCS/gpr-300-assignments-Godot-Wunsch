using Godot;
using System;
using System.Collections.Generic;
using Gizmo3DPlugin;
using Godot.NativeInterop;
using ImGuiNET;

public partial class A5_Testing : Node
{
    //Exported Fields
    [Export] private Path3D _path;
    [Export] private MeshInstance3D _meshInstance;
    [Export] private Node3D _sphereHolder;
    
    //Runtime Objects
    private CsgBox3D _cube;
    private Gizmo3D _gizmo = new();
    Curve3D _curve = new();
    
    //Runtime Variables
    private float _progress = 0f;
    private float _speed = 0.1f;
    private int _selectedSphereIndex = -1;
    
    //Dictionaries
    private Godot.Collections.Dictionary<int, MeshInstance3D> _spheres = new();
    private Godot.Collections.Dictionary<int, Vector3> _lastSpherePositions = new();
    private Godot.Collections.Dictionary<int, Vector3> _originalInHandles = new();
    private Godot.Collections.Dictionary<int, Vector3> _originalOutHandles = new();
    
    //State Tracking
    private bool _isPaused;
    private bool _isBouncing = true;
    private bool _isInit;
    private bool _showDebugUI;

    //Lifecycle Functions
    public override void _Ready()
    {
        if (_path == null)
        {
           RuntimeConsole.LogError("Path is null");
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
    public override void _Process(double delta)
    {
        if (_curve == null || _cube == null || _curve.PointCount == 0)
            return;
        
        SyncCurveWithSpheres();

        if (!_isPaused)
        {
            _progress += (float)delta * _speed;
            if (_progress > 1f)
                _progress = 0f;
        }

        float curveLength = _curve.GetBakedLength();

        // Sample position and ahead position for direction
        Vector3 newPos = _curve.SampleBaked(_progress * curveLength, true);
        float aheadProgress = _progress + 0.01f;
        if (aheadProgress > 1f) aheadProgress = 0f;

        Vector3 nextPos = _curve.SampleBaked(aheadProgress * curveLength, true);
        Vector3 direction = (nextPos - newPos).Normalized();

      
        float bounceHeight = 0.08f; // Small hop
        float bounceSpeed = 4f; // How fast Dirpy bounces
        float bounceOffset = Mathf.Sin(_progress * Mathf.Pi * 2 * bounceSpeed) * bounceHeight;

        // Apply new position with bounce
        if (_isBouncing)
        {
            _cube.Position = new Vector3(newPos.X, newPos.Y + bounceOffset, newPos.Z);
        }

        else
        {
            _cube.Position = new Vector3(newPos.X, newPos.Y, newPos.Z);
        }
        

        //Make Dirpy Forward
        if (direction.Length() > 0.01f) 
        {
            _cube.LookAt(newPos - direction, Vector3.Up);
        }

        DrawCurve();
        
        if(_showDebugUI)
            DrawImGui();
       
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!RuntimeConsole.IsTyping)
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
                switch (keyEvent.Keycode)
                {
                    case Key.Up:
                        Vector3 newPoint = _spheres.Count > 0
                            ? _spheres[_spheres.Count - 1].Position + new Vector3(0.5f, 0, 0)
                            : new Vector3(0, 0, 0);
                        AddPoint(newPoint);
                        break;

                    case Key.Down:
                        if (_selectedSphereIndex != -1)
                        {
                            RemovePoint(_selectedSphereIndex);
                            _selectedSphereIndex = -1;
                        }
                        break;

                    case Key.Key1:
                        _gizmo.Mode = Gizmo3D.ToolMode.Move;
                        RuntimeConsole.LogMessage("Gizmo Mode : Move");
                        break;

                    case Key.Key2:
                        _gizmo.Mode = Gizmo3D.ToolMode.Scale;
                        RuntimeConsole.LogMessage("Gizmo Mode : Scale");
                        break;

                    case Key.Key3:
                        _gizmo.Mode = Gizmo3D.ToolMode.Rotate;
                        RuntimeConsole.LogMessage("Gizmo Mode : Rotate");
                        break;

                    case Key.Escape:
                        DeselectSphere();
                        break;

                    case Key.Space:
                        TogglePause();
                        break;

                    case Key.R:
                        ResetCurve();
                        break;

                    case Key.Backslash:
                        _showDebugUI = !_showDebugUI;
                        break;
                }
            }
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
        
        _cube.QueueFree();
        RuntimeConsole.ClearLog();
    }
    
    //Logic Control Functions
    public void TogglePause()
    {
        _isPaused = !_isPaused;
        RuntimeConsole.LogMessage(_isPaused ? "Paused" : "Resumed");
    }

    void ToggleUI()
    {
        _showDebugUI = !_showDebugUI;
        RuntimeConsole.Toggle();
    }
    public void ResetCurve()
    {
        if (!_isInit)
        {
            RuntimeConsole.LogMessage("Setting Curve");
        }
        else
        {
            RuntimeConsole.LogMessage("Resetting Curve");
        }

        // Deselect selected sphere and gizmo
        DeselectSphere();

        // Clear existing curve
        _curve.ClearPoints();

        // Remove all spheres
        foreach (var sphere in _spheres.Values)
        {
            sphere.QueueFree();
        }
        _spheres.Clear();
        _lastSpherePositions.Clear();
        _originalInHandles.Clear();
        _originalOutHandles.Clear();

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
        RuntimeConsole.LogMessage("Curve Reset");
    }
    
    //Curve Creation and Rendering
    private void DrawCurve()
    {
        ImmediateMesh immediateMesh = new ImmediateMesh();
        immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

        float length = _curve.GetBakedLength();
        int steps = 300; // Higher value = smoother curve

        // Define distinct colors
        Color[] colors = { Colors.Green, Colors.Blue, Colors.Yellow, Colors.Red, Colors.Magenta };

        for (int i = 0; i < steps; i++)
        {
            float t1 = (i / (float)steps) * length;
            float t2 = ((i + 1) / (float)steps) * length;

            Vector3 start = _curve.SampleBaked(t1);
            Vector3 end = _curve.SampleBaked(t2);

            Color segmentColor = colors[i % colors.Length]; // Pick a color based on segment index

            immediateMesh.SurfaceSetColor(segmentColor);
            immediateMesh.SurfaceAddVertex(start);

            immediateMesh.SurfaceSetColor(segmentColor);
            immediateMesh.SurfaceAddVertex(end);
        }

        immediateMesh.SurfaceEnd();

        // 🟢 Assign the custom vertex color shader material
        if (_meshInstance.MaterialOverride == null)
        {
            _meshInstance.MaterialOverride = CreateVertexColorMaterial();
        }

        _meshInstance.Mesh = immediateMesh;
    }
    private ShaderMaterial CreateVertexColorMaterial()
    {
        Shader shader = new Shader();
        shader.Code = @"
        shader_type spatial;
        render_mode unshaded;
        
        void fragment() {
            ALBEDO = COLOR.rgb;
        }
    ";

        ShaderMaterial shaderMat = new ShaderMaterial();
        shaderMat.Shader = shader;

        return shaderMat;
    }
    private void AddCurvePoint(Vector3 position, Vector3 inHandle, Vector3 outHandle)
    {
        _curve.AddPoint(position);
        int index = _curve.PointCount - 1;
        _curve.SetPointIn(index, inHandle);
        _curve.SetPointOut(index, outHandle);

       
        _originalInHandles[index] = inHandle;
        _originalOutHandles[index] = outHandle;
    }
    private void SyncCurveWithSpheres()
    {
        foreach (var kvp in _spheres)
        {
            int index = kvp.Key;
            MeshInstance3D sphere = kvp.Value;

            if (index >= _curve.PointCount) continue; // Skip invalid points

            //Position Syncing (Already Works)
            Vector3 newSpherePos = sphere.GlobalTransform.Origin;
            if (_lastSpherePositions.ContainsKey(index) && _lastSpherePositions[index] != newSpherePos)
            {
                _curve.SetPointPosition(index, newSpherePos);
                _lastSpherePositions[index] = newSpherePos;
            }

            //Scale Syncing
            Vector3 sphereScale = sphere.Scale;
            if (!_originalInHandles.ContainsKey(index) || !_originalOutHandles.ContainsKey(index))
                continue;

            float scaleMultiplier = 1.5f;
            float scaleFactor = sphere.Scale.Length();
            Vector3 scaledInHandle = _originalInHandles[index].Normalized() * _originalInHandles[index].Length() * scaleFactor * scaleMultiplier;
            Vector3 scaledOutHandle = _originalOutHandles[index].Normalized() * _originalOutHandles[index].Length() * scaleFactor * scaleMultiplier;

            _curve.SetPointIn(index, scaledInHandle);
            _curve.SetPointOut(index, scaledOutHandle);

            //Rotation Syncing (Only update when needed)
            Quaternion sphereRotation = sphere.GlobalTransform.Basis.GetRotationQuaternion();
            Vector3 lastRotation = _lastSpherePositions.ContainsKey(index) ? _lastSpherePositions[index] : Vector3.Zero;
            Vector3 newRotation = sphereRotation.GetEuler();

            if (!lastRotation.IsEqualApprox(newRotation)) // Only update when actually rotated
            {
                Basis rotatedBasis = new Basis(sphereRotation);

                Vector3 rotatedInHandle = rotatedBasis * _originalInHandles[index];
                Vector3 rotatedOutHandle = rotatedBasis * _originalOutHandles[index];

                _curve.SetPointIn(index, rotatedInHandle);
                _curve.SetPointOut(index, rotatedOutHandle);

                _lastSpherePositions[index] = newRotation;
            }
        }
    }
    
    //Point Management
    public void AddPoint(Vector3 position)
    {
        int index = _curve.PointCount;
        Vector3 inHandle = Vector3.Zero;
        Vector3 outHandle = Vector3.Zero;

        // If there's at least one point, calculate tangents (Was Causing severe issues earlier; whoops)
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
        
        _originalInHandles[index] = inHandle;
        _originalOutHandles[index] = outHandle;

        RuntimeConsole.LogMessage("Point added at " + position.ToString());
        DrawCurve();
    }
    public void RemovePoint(int index)
    {
        if (index < 0 || index >= _curve.PointCount)
        {
            RuntimeConsole.LogError($"Invalid index {index} for removal.");
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
        RuntimeConsole.LogMessage("Point " + index + " removed");
        DrawCurve();
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
    
    //Selection and Gizmo Control
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
            var material = sphere.MaterialOverride as StandardMaterial3D;
            material.AlbedoColor = Colors.DarkRed;
            _gizmo.Deselect(sphere);
            
            if(sphere != null)
                RuntimeConsole.LogMessage("Deselected Sphere");
        }
        
        _selectedSphereIndex = -1;
        _gizmo.Deselect(_path);
    }
    
    //Visual and Animation Extras
   private void AddMovingObject()
{
    // Create the cube body
    var cube = new CsgBox3D();
    cube.Size = new Vector3(0.2f, 0.2f, 0.2f);

    StandardMaterial3D cubeMat = new StandardMaterial3D();
    cubeMat.AlbedoColor = Colors.Cyan;
    cube.Material = cubeMat;

    //Mouth
    var mouthSphere = new CsgSphere3D();
    mouthSphere.Radius = 0.08f;
    mouthSphere.Position = new Vector3(0, 0, 0.12f);
    mouthSphere.RotationDegrees = new Vector3(90, 0, 0);
    mouthSphere.Operation = CsgPrimitive3D.OperationEnum.Subtraction;
    cube.AddChild(mouthSphere);

    //Mouth Animation 
    var timer = new Timer { WaitTime = 0.05, Autostart = true, OneShot = false };
    AddChild(timer);

    float phase = 0f;
    timer.Timeout += () =>
    {
        if (_isPaused) return;
        
        phase += 0.2f;
        float scaleY = Mathf.Lerp(0.05f, 0.8f, (Mathf.Sin(phase) + 1f) * 0.5f);
        mouthSphere.Scale = new Vector3(1f, scaleY, 1f);
    };


    //Eyes
    var scleraMat = new StandardMaterial3D { AlbedoColor = Colors.White };
    var pupilMat = new StandardMaterial3D { AlbedoColor = Colors.Black };

    var leftEye = new CsgSphere3D
    {
        Radius = 0.04f,
        Position = new Vector3(-0.05f, 0.05f, 0.11f),
        Material = scleraMat
    };
    var leftPupil = new MeshInstance3D
    {
        Mesh = new SphereMesh { Radius = 0.02f, Height = 0.04f },
        Position = new Vector3(0, 0, 0.025f),
        MaterialOverride = pupilMat
    };
    leftEye.AddChild(leftPupil);

    var rightEye = new CsgSphere3D
    {
        Radius = 0.04f,
        Position = new Vector3(0.05f, 0.05f, 0.11f),
        Material = scleraMat
    };
    var rightPupil = new MeshInstance3D
    {
        Mesh = new SphereMesh { Radius = 0.02f, Height = 0.04f },
        Position = new Vector3(0, 0, 0.025f),
        MaterialOverride = pupilMat
    };
    rightEye.AddChild(rightPupil);

    cube.AddChild(leftEye);
    cube.AddChild(rightEye);

    // Assign Dirpy
    _cube = cube;
    AddChild(_cube);
}

    void DrawImGui()
    {

        if (ImGui.Begin("Sphere Info"))
        {
            foreach(var sphere in _spheres)
                ImGui.Text("Sphere " + sphere.Key.ToString() + " pos : " + sphere.Value.Position.ToString());
        }

        ImGui.End();

        if (ImGui.Begin("Exposed Variables"))
        {
            // Speed Slider
            ImGui.SliderFloat("Speed", ref _speed, 0f, 2f);

            // Reset Speed Button
            if (ImGui.Button("Reset Speed"))
            {
                _speed = 0.1f;
                RuntimeConsole.LogMessage("Speed reset to 0.1");
            }

            // Pause Toggle
            bool pauseToggle = _isPaused;
            if (ImGui.Checkbox("Paused", ref pauseToggle) && pauseToggle != _isPaused)
            {
                TogglePause();
            }

            // Bounce Toggle
            ImGui.Checkbox("Bouncing", ref _isBouncing);

            // Show Debug UI Toggle
            ImGui.Checkbox("Show Debug UI", ref _showDebugUI);
        }
        ImGui.End();

        
        RuntimeConsole.Draw();
    }
}