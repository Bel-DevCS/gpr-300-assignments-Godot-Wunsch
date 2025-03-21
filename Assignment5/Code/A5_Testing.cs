using Godot;
using System;
using System.Collections.Generic;
using Gizmo3DPlugin;
using Godot.NativeInterop;
public partial class A5_Testing : Node
{
    //Exported Fields
    [Export] private Path3D _path;
    [Export] private MeshInstance3D _meshInstance;
    [Export] private Node3D _sphereHolder;
    
    //Runtime Objects
    private CsgBox3D _cube;
    private Gizmo3D _gizmo = new();
    Curve3D _curve = new Curve3D();
    
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

    //Lifecycle Functions
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
            else if (keyEvent.Keycode == Key.Key1) // Toggle to Move Mode
            {
                _gizmo.Mode = Gizmo3D.ToolMode.Move;
                GD.Print("Gizmo Mode: Move");
            }
            else if (keyEvent.Keycode == Key.Key2) // Toggle to Scale Mode
            {
                _gizmo.Mode = Gizmo3D.ToolMode.Scale;
                GD.Print("Gizmo Mode: Scale");
            }
            else if (keyEvent.Keycode == Key.Key3) // Toggle to Rotate Mode
            {
                _gizmo.Mode = Gizmo3D.ToolMode.Rotate;
                GD.Print("Gizmo Mode: Rotate");
            }
            else if (keyEvent.Keycode == Key.Escape) // 🟢 Deselect sphere on Escape key
            {
                DeselectSphere();
                GD.Print("Deselected sphere.");
            }
            
            else if (keyEvent.Keycode == Key.Space)
            {
                TogglePause();
            }
            
            else if (keyEvent.Keycode == Key.R)
            {
                ResetCurve();
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
    }
    
    //Logic Control Functions
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
        GD.Print("Curve Reset!");
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
        
        _originalInHandles[index] = inHandle;
        _originalOutHandles[index] = outHandle;

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

        // Create the left eye (CSG Sphere)
        var leftEye = new CsgSphere3D();
        leftEye.Radius = 0.04f;
        leftEye.Position = new Vector3(-0.05f, 0.05f, 0.11f); 

        StandardMaterial3D scleraMat = new StandardMaterial3D();
        scleraMat.AlbedoColor = Colors.White;
        leftEye.Material = scleraMat;

        // Create left pupil (MeshInstance3D instead of CSG)
        var leftPupil = new MeshInstance3D();
        leftPupil.Mesh = new SphereMesh()
        {
            Radius = 0.02f, // Smaller than sclera
            Height = 0.04f
        };
        leftPupil.Position = new Vector3(0, 0, 0.025f); // Move slightly forward inside eye

        StandardMaterial3D pupilMat = new StandardMaterial3D();
        pupilMat.AlbedoColor = Colors.Black;
        leftPupil.MaterialOverride = pupilMat;

        // Parent pupil to eye
        leftEye.AddChild(leftPupil);

        // Create the right eye (Duplicate left eye)
        var rightEye = new CsgSphere3D();
        rightEye.Radius = 0.04f;
        rightEye.Position = new Vector3(0.05f, 0.05f, 0.11f);
        rightEye.Material = scleraMat;

        // Create right pupil (Another MeshInstance3D)
        var rightPupil = new MeshInstance3D();
        rightPupil.Mesh = new SphereMesh()
        {
            Radius = 0.02f,
            Height = 0.04f
        };
        rightPupil.Position = new Vector3(0, 0, 0.025f);
        rightPupil.MaterialOverride = pupilMat;

        // Parent pupil to right eye
        rightEye.AddChild(rightPupil);

        // Parent eyes to cube
        cube.AddChild(leftEye);
        cube.AddChild(rightEye);

        // Assign to class variable for movement
        _cube = cube;
        AddChild(_cube);
    }
}