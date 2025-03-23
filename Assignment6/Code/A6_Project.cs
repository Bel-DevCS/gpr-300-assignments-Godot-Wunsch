using Godot;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vector3 = Godot.Vector3;

public partial class A6_Project : Node3D
{
    private Skeleton _skeleton;
    private Dictionary<Joint, MeshInstance3D> _jointVisuals = new();


    public override void _Ready()
    {
        _skeleton = new Skeleton();
        CreateVisualJoints();
    }

    public override void _Process(double delta)
    {
        _skeleton.Update();
        UpdateVisualJoints();
        DrawImGui();
    }

    private void DrawImGui()
    {
        if (ImGui.Begin("Skeleton Joints"))
        {
            foreach (var joint in _skeleton.AllJoints)
            {
                if (ImGui.TreeNode(joint.Name))
                {
                    // Convert Godot Vector3 -> System.Numerics.Vector3
                    var pos = joint.LocalPosition.ToNumerics();
                    var rot = joint.LocalRotation.ToNumerics();
                    var scale = joint.LocalScale.ToNumerics();

                    if (ImGui.DragFloat3($"Position##{joint.Name}", ref pos, 0.01f))
                        joint.LocalPosition = pos.ToGodot();

                    if (ImGui.DragFloat3($"Rotation##{joint.Name}", ref rot, 0.5f))
                        joint.LocalRotation = rot.ToGodot();

                    if (ImGui.DragFloat3($"Scale##{joint.Name}", ref scale, 0.01f))
                        joint.LocalScale = scale.ToGodot();


                    ImGui.TreePop();
                }
            }
        }
        
        ImGui.End();
       
    }
    
    private void CreateVisualJoints()
    {
        foreach (var joint in _skeleton.AllJoints)
        {
            var meshInstance = new MeshInstance3D();

            // Use a basic cube
            meshInstance.Mesh = new BoxMesh
            {
                Size = new Vector3(0.1f, 0.1f, 0.1f)
            };

            // Optional: give it a material so it’s easier to see
            var material = new StandardMaterial3D
            {
                AlbedoColor = new Color(1, 1, 1, 1)
            };
            meshInstance.MaterialOverride = material;

            AddChild(meshInstance);
            _jointVisuals[joint] = meshInstance;
        }
    }

    private void UpdateVisualJoints()
    {
        foreach (var (joint, visual) in _jointVisuals)
        {
            visual.GlobalTransform = joint.GlobalTransform;
        }
    }


    public override void _PhysicsProcess(double delta)
    {
      
    }

}