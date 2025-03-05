using Godot;
using System;
using static Godot.Input.MouseModeEnum;

public partial class A4_CameraController : Camera3D
{
    [Export] public float Speed = 5.0f;
    [Export] public float MouseSensitivity = 0.1f;
    private bool _isRightMouseDown = false;

    [Export] private Node3D rootNode; // ✅ Reference to MainScene to manage selections
    [Export] private Label selectionStatusLabel; // ✅ Reference to the UI Label for selection status

    private A4_AnimatedObject selectedObject = null;
    private Control uiPanel;

    private bool canSelectObjects = true; // ✅ NEW: Determines if we can select objects

    public override void _Ready()
    {
        UpdateSelectionLabel();
    }

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

            // ✅ Left-click should only work if selection is enabled
            if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed && canSelectObjects)
            {
                HandleClick();
            }
        }

        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            // ✅ Press Escape to allow selecting objects again
            if (keyEvent.Keycode == Key.Escape)
            {
                DeselectAllObjects();
                canSelectObjects = true;
                GD.Print("[Camera] Object selection re-enabled.");
                UpdateSelectionLabel(); // ✅ Update UI when selection is enabled again
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
        // ✅ Ensure `uiPanel` is assigned before checking it
        if (uiPanel != null && IsPositionInsidePanel(uiPanel)) return;

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
                    return;
                }
            }
        }
    }

    private bool IsPositionInsidePanel(Control panel)
    {
        Vector2 mousePos = panel.GetLocalMousePosition();
        return mousePos.X >= 0 && mousePos.Y >= 0 && mousePos.X <= panel.Size.X && mousePos.Y <= panel.Size.Y;
    }

    private void SelectObject(A4_AnimatedObject newSelection)
    {
        if (selectedObject != null && selectedObject != newSelection)
        {
            selectedObject.SetUIVisibility(false);
        }

        selectedObject = newSelection;
        selectedObject.SetUIVisibility(true);

        // ✅ Ensure `uiPanel` is updated properly
        uiPanel = selectedObject?.GetControl();

        // ✅ Disable further selection while the UI is open
        canSelectObjects = false;

        // ✅ Update UI to show that an object is currently being edited
        UpdateSelectionLabel();
    }

    private void DeselectAllObjects()
    {
        if (selectedObject != null)
        {
            selectedObject.SetUIVisibility(false);
            selectedObject = null;
            uiPanel = null;
        }

        // ✅ Update UI to show selection is available again
        UpdateSelectionLabel();
    }

    private void UpdateSelectionLabel()
    {
        if (selectionStatusLabel != null)
        {
            if (canSelectObjects)
            {
                selectionStatusLabel.Text = "Select an Object";
            }
            else
            {
                selectionStatusLabel.Text = "Currently Editing [Press Esc to Enable Selection]";
            }
        }
    }
}
