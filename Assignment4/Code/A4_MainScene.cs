using Godot;
using System.Collections.Generic;

public partial class A4_MainScene : Node3D
{
    [Export] private PackedScene AnimatedObjectScene; // Assign in the Inspector
    [Export] private A4_CameraController camera;

    private List<A4_AnimatedObject> animatedObjects = new();

    [Export] private Button _Play;
    [Export] private Button _Pause;
    [Export] private CheckButton _Loop;
    [Export] private SpinBox _PlaybackSpeed;

    public override void _Ready()
    {
        if (AnimatedObjectScene == null)
        {
            GD.PrintErr("[MainScene] AnimatedObjectScene is not assigned in the Inspector!");
            return;
        }

        if (camera == null)
        {
            camera = GetViewport().GetCamera3D() as A4_CameraController;
        }

        if (camera == null)
        {
            GD.PrintErr("[MainScene] No CameraController found! Click handling will not work.");
        }

        AddAnimatedObject(new Vector3(0, 0, 0));
        AddAnimatedObject(new Vector3(5, 0, 0));
        AddAnimatedObject(new Vector3(-5, 0, 0));

        // 🔹 Connect UI Buttons
        if (_Play != null) _Play.Pressed += RunAnimations;
        else GD.PrintErr("[MainScene] _Play button is not assigned!");

        if (_Pause != null) _Pause.Pressed += PauseAnimations;
        else GD.PrintErr("[MainScene] _Pause button is not assigned!");

        if (_Loop != null) _Loop.Toggled += LoopAnimations;
        else GD.PrintErr("[MainScene] _Loop toggle is not assigned!");

        if (_PlaybackSpeed != null) _PlaybackSpeed.ValueChanged += AdjustSpeed;
        else GD.PrintErr("[MainScene] _PlaybackSpeed spinbox is not assigned!");
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
        newObject.GetObjectSwitcher().MoveModels(position);

        animatedObjects.Add(newObject);
    }

    void RunAnimations()
    {
        GD.Print("[MainScene] Playing animations.");
        foreach (var anim in animatedObjects)
            anim.PlayAnimation();
    }

    void PauseAnimations()
    {
        GD.Print("[MainScene] Pausing animations.");
        foreach (var anim in animatedObjects)
            anim.PauseAnimation();
    }

    void LoopAnimations(bool enabled)
    {
        GD.Print($"[MainScene] Looping animations: {enabled}");
        foreach (var anim in animatedObjects)
            anim.SetLoop(enabled);
    }

    void AdjustSpeed(double value)
    {
        GD.Print($"[MainScene] Adjusting playback speed: {value}");
        foreach (var anim in animatedObjects)
            anim.SetPlaybackSpeed((float)value);
    }
}
