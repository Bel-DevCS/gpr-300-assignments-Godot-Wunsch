using Godot;
using System.Collections.Generic;

public partial class A4_MainScene : Node3D
{
    [Export] private PackedScene AnimatedObjectScene; // Assign in the Inspector
    [Export] private A4_CameraController camera; // ✅ Ensure Camera Controller is correctly assigned

    private List<A4_AnimatedObject> animatedObjects = new();

    public override void _Ready()
    {
        if (AnimatedObjectScene == null)
        {
            GD.PrintErr("[MainScene] AnimatedObjectScene is not assigned in the Inspector!");
            return;
        }

        if (camera == null)
        {
            // ✅ Try to fetch the Camera if it's not assigned manually.
            camera = GetViewport().GetCamera3D() as A4_CameraController;
        }

        if (camera == null)
        {
            GD.PrintErr("[MainScene] No CameraController found! Click handling will not work.");
        }

        AddAnimatedObject(new Vector3(0, 0, 0));
    }

    public void AddAnimatedObject(Vector3 position)
    {
        if (AnimatedObjectScene == null)
        {
            GD.PrintErr("[MainScene] AnimatedObjectScene is not assigned!");
            return;
        }

        A4_AnimatedObject newObject = AnimatedObjectScene.Instantiate<A4_AnimatedObject>();
        AddChild(newObject);
        newObject.GlobalPosition = position;

        animatedObjects.Add(newObject);
    }
}