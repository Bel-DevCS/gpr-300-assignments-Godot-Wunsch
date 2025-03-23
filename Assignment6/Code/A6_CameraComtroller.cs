using Godot;
using System;
using static Godot.Input.MouseModeEnum;

public partial class A6_CameraController : Camera3D
{
    [Export]
    public float Speed = 5.0f;
    
    [Export]
    public float MouseSensitivity = 0.1f;

    private bool _isRightMouseDown = false;
    
    public override void _Process(double delta)
    {
        Vector3 direction = Vector3.Zero;

        if (!RuntimeConsole.IsTyping)
        {
            // Forward/backward: In Godot, forward is negative Z.
            if (Input.IsActionPressed("move_forward"))
                direction -= Transform.Basis.Z;
            if (Input.IsActionPressed("move_back"))
                direction += Transform.Basis.Z;
        
            // Left/right movement.
            if (Input.IsActionPressed("move_left"))
                direction -= Transform.Basis.X;
            if (Input.IsActionPressed("move_right"))
                direction += Transform.Basis.X;
        
            // Up/down movement.
            if (Input.IsActionPressed("move_up"))
                direction += Transform.Basis.Y;
            if (Input.IsActionPressed("move_down"))
                direction -= Transform.Basis.Y;
        
            if (direction != Vector3.Zero)
            {
                // Normalize to prevent faster diagonal movement.
                direction = direction.Normalized();
                Position += direction * Speed * (float)delta;
            }
        }
    }
    
    public override void _Input(InputEvent @event)
    {
        // Handle right mouse button press/release.
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                _isRightMouseDown = mouseButton.Pressed;
                // Capture the mouse when right button is down; release it otherwise.
                Input.SetMouseMode(_isRightMouseDown ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible);
            }
        }
        
        // Handle mouse motion when right mouse button is held.
        if (_isRightMouseDown && @event is InputEventMouseMotion mouseMotion)
        {
            Vector3 rotationDeg = RotationDegrees;
            // Update yaw (Y-axis) with Relative.X and pitch (X-axis) with Relative.Y.
            rotationDeg.Y -= mouseMotion.Relative.X * MouseSensitivity;
            rotationDeg.X -= mouseMotion.Relative.Y * MouseSensitivity;
            // Clamp pitch so the camera doesn't flip.
            rotationDeg.X = Mathf.Clamp(rotationDeg.X, -90, 90);
            RotationDegrees = rotationDeg;
        }
    }
}
