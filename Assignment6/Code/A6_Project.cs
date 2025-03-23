using Godot;
using ImGuiNET;
using System;
using System.Numerics;

public partial class A6_Project : Node3D
{
    private Skeleton _skeleton;

    public override void _Ready()
    {
        _skeleton = new Skeleton();
    }

    public override void _Process(double delta)
    {
        _skeleton.Update();
        DrawImGui();
    }

    private void DrawImGui()
    {
        foreach (var joint in _skeleton.AllJoints)
        {
            if (ImGui.TreeNode(joint.Name))
            {
                // Convert Godot Vector3 -> System.Numerics.Vector3
                var pos = joint.LocalPosition.ToNumerics();
                var rot = joint.LocalRotation.ToNumerics();
                var scale = joint.LocalScale.ToNumerics();

                if (ImGui.DragFloat3($"Position##{joint.Name}", ref pos))
                    joint.LocalPosition = pos.ToGodot();

                if (ImGui.DragFloat3($"Rotation##{joint.Name}", ref rot))
                    joint.LocalRotation = rot.ToGodot();

                if (ImGui.DragFloat3($"Scale##{joint.Name}", ref scale, 0.01f))
                    joint.LocalScale = scale.ToGodot();

                ImGui.TreePop();
            }
        }
    }


    public override void _PhysicsProcess(double delta)
    {
      
    }

}