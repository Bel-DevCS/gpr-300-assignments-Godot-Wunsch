using Godot;
using System;
using static Godot.Input.MouseModeEnum;

public partial class A4_CameraController : Camera3D
{
    [Export] public float Speed = 5.0f;
    [Export] public float MouseSensitivity = 0.1f;
    private bool _isRightMouseDown = false;
    
    [Export] private Node3D rootNode; // ✅ Reference to MainScene to manage selections

    private A4_AnimatedObject selectedObject = null;

    public override void _Process(double delta)
    {
        Vector3 direction = Vector3.Zero;
        
        if (Input.IsActionPressed("move_forward"))
            direction -= Transform.Basis.Z;
        if (Input.IsActionPressed("move_back"))
            direction += Transform.Basis.Z;
        
        if (Input.IsActionPressed("move_left"))
            direction -= Transform.Basis.X;
        if (Input.IsActionPressed("move_right"))
            direction += Transform.Basis.X;
        
        if (Input.IsActionPressed("move_up"))
            direction += Transform.Basis.Y;
        if (Input.IsActionPressed("move_down"))
            direction -= Transform.Basis.Y;
        
        if (direction != Vector3.Zero)
        {
            direction = direction.Normalized();
            Position += direction * Speed * (float)delta;
        }
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                _isRightMouseDown = mouseButton.Pressed;
                Input.SetMouseMode(_isRightMouseDown ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible);
            }
        
            //
            if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
            {
                HandleClick(); //
            }
        }

        if (_isRightMouseDown && @event is InputEventMouseMotion mouseMotion)
        {
            Vector3 rotationDeg = RotationDegrees;
            rotationDeg.Y -= mouseMotion.Relative.X * MouseSensitivity;
            rotationDeg.X -= mouseMotion.Relative.Y * MouseSensitivity;
            rotationDeg.X = Mathf.Clamp(rotationDeg.X, -90, 90);
            RotationDegrees = rotationDeg;
        }
    }


    private void HandleClick()
    {
        if (rootNode == null)
        {
            GD.PrintErr("[Camera] RootNode (MainScene) is not assigned!");
            return;
        }

        var spaceState = GetViewport().World3D.DirectSpaceState;
        Vector2 mousePosition = GetViewport().GetMousePosition();
        var query = PhysicsRayQueryParameters3D.Create(
            ProjectRayOrigin(mousePosition),
            ProjectRayOrigin(mousePosition) + ProjectRayNormal(mousePosition) * 1000
        );

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0 && result.TryGetValue("collider", out Variant colliderVariant))
        {
            GodotObject colliderObject = colliderVariant.AsGodotObject();
            
            if (colliderObject is StaticBody3D staticBody)
            {
                if (staticBody.GetParent() is A4_AnimatedObject clickedObject)
                {
                    SelectObject(clickedObject);
                    GD.Print($"[Raycast] Hit: {colliderObject.GetType()} - {colliderObject.GetType().ToString()}");
                    return;
                }
            }
        }

        DeselectAllObjects();
    }


    private void SelectObject(A4_AnimatedObject newSelection)
    {
        if (selectedObject != null && selectedObject != newSelection)
        {
            selectedObject.SetUIVisibility(false);
        }

        selectedObject = newSelection;
        selectedObject.SetUIVisibility(true);
    }

    private void DeselectAllObjects()
    {
        if (selectedObject != null)
        {
            selectedObject.SetUIVisibility(false);
            selectedObject = null;
        }
    }
}
