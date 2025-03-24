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
    
    private ImmediateMesh _boneLines;
    private MeshInstance3D _boneRenderer;



  public override void _Ready()
{
    _skeleton = new Skeleton();

    // Core body
    var torso = _skeleton.Root;
    torso.Name = "Torso";
    torso.LocalPosition = new Vector3(0, 0, 0);

    var neck = _skeleton.CreateJoint("Neck", torso);
    neck.LocalPosition = new Vector3(0, 0.3f, 0);

    var head = _skeleton.CreateJoint("Head", neck);
    head.LocalPosition = new Vector3(0, 0.2f, 0);

    // Left Arm
    var lShoulder = _skeleton.CreateJoint("LeftShoulder", torso);
    lShoulder.LocalPosition = new Vector3(-0.3f, 0.25f, 0);
    
    var lElbow = _skeleton.CreateJoint("LeftElbow", lShoulder);
    lElbow.LocalPosition = new Vector3(-0.25f, 0, 0);

    var lWrist = _skeleton.CreateJoint("LeftWrist", lElbow);
    lWrist.LocalPosition = new Vector3(-0.25f, 0, 0);

    var lHand = _skeleton.CreateJoint("LeftHand", lWrist);
    lHand.LocalPosition = new Vector3(-0.1f, 0, 0);

    var lThumb = _skeleton.CreateJoint("LeftThumb", lHand);
    lThumb.LocalPosition = new Vector3(-0.05f, 0.02f, 0);

    var lFingers = _skeleton.CreateJoint("LeftFingers", lHand);
    lFingers.LocalPosition = new Vector3(-0.07f, -0.02f, 0);

    // Right Arm
    var rShoulder = _skeleton.CreateJoint("RightShoulder", torso);
    rShoulder.LocalPosition = new Vector3(0.3f, 0.25f, 0);

    var rElbow = _skeleton.CreateJoint("RightElbow", rShoulder);
    rElbow.LocalPosition = new Vector3(0.25f, 0, 0);

    var rWrist = _skeleton.CreateJoint("RightWrist", rElbow);
    rWrist.LocalPosition = new Vector3(0.25f, 0, 0);

    var rHand = _skeleton.CreateJoint("RightHand", rWrist);
    rHand.LocalPosition = new Vector3(0.1f, 0, 0);

    var rThumb = _skeleton.CreateJoint("RightThumb", rHand);
    rThumb.LocalPosition = new Vector3(0.05f, 0.02f, 0);

    var rFingers = _skeleton.CreateJoint("RightFingers", rHand);
    rFingers.LocalPosition = new Vector3(0.07f, -0.02f, 0);

    // Left Leg
    var lHip = _skeleton.CreateJoint("LeftHip", torso);
    lHip.LocalPosition = new Vector3(-0.15f, -0.3f, 0);

    var lKnee = _skeleton.CreateJoint("LeftKnee", lHip);
    lKnee.LocalPosition = new Vector3(0, -0.4f, 0);

    var lAnkle = _skeleton.CreateJoint("LeftAnkle", lKnee);
    lAnkle.LocalPosition = new Vector3(0, -0.4f, 0);

    var lFoot = _skeleton.CreateJoint("LeftFoot", lAnkle);
    lFoot.LocalPosition = new Vector3(0, -0.05f, 0.1f);

    // Right Leg
    var rHip = _skeleton.CreateJoint("RightHip", torso);
    rHip.LocalPosition = new Vector3(0.15f, -0.3f, 0);

    var rKnee = _skeleton.CreateJoint("RightKnee", rHip);
    rKnee.LocalPosition = new Vector3(0, -0.4f, 0);

    var rAnkle = _skeleton.CreateJoint("RightAnkle", rKnee);
    rAnkle.LocalPosition = new Vector3(0, -0.4f, 0);

    var rFoot = _skeleton.CreateJoint("RightFoot", rAnkle);
    rFoot.LocalPosition = new Vector3(0, -0.05f, 0.1f);
    
    CreateVisualJoints();
    
    _boneLines = new ImmediateMesh();
    _boneRenderer = new MeshInstance3D
    {
        Mesh = _boneLines,
        CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        MaterialOverride = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 1f, 0.5f)
        }
    };
    AddChild(_boneRenderer);

}


    public override void _Process(double delta)
    {
        _skeleton.Update();
        UpdateVisualJoints();
        DrawBoneLines();
        DrawImGui();
    }

    private void DrawImGui()
    {
        if (!ImGui.Begin("Skeleton Joints")) return;

        DrawJointRecursive(_skeleton.Root);

        ImGui.End();
    }



    
   private void CreateVisualJoints()
{
    foreach (var joint in _skeleton.AllJoints)
    {
        var meshInstance = new MeshInstance3D();
        PrimitiveMesh mesh;

        string name = joint.Name.ToLowerInvariant();

        // HEAD
        if (name.Contains("head"))
        {
            mesh = new SphereMesh
            {
                Radius = 0.25f,
                Height = 0.25f,
                RadialSegments = 12,
                Rings = 6
            };
        }
        // NECK or joints connecting to head
        else if (name.Contains("neck"))
        {
            mesh = new CylinderMesh
            {
                TopRadius = 0.05f,
                BottomRadius = 0.05f,
                Height = 0.1f,
                RadialSegments = 8
            };
        }
        // TORSO
        else if (name.Contains("torso"))
        {
            mesh = new CylinderMesh
            {
                TopRadius = 0.1f,
                BottomRadius = 0.1f,
                Height = 0.4f,
                RadialSegments = 12
            };
        }
        // SHOULDERS, ELBOWS, KNEES (pivot joints)
        else if (name.Contains("shoulder") || name.Contains("elbow") || name.Contains("knee"))
        {
            mesh = new SphereMesh
            {
                Radius = 0.07f,
                Height = 0.07f,
                RadialSegments = 8,
                Rings = 4
            };
        }
        // ARMS / LEGS (longer segments)
        else if (name.Contains("arm") || name.Contains("leg") || name.Contains("hip"))
        {
            mesh = new CapsuleMesh
            {
                Radius = 0.06f,
                Height = 0.2f,
                RadialSegments = 8,
                Rings = 4
            };
        }
        // WRISTS / ANKLES
        else if (name.Contains("wrist") || name.Contains("ankle"))
        {
            mesh = new SphereMesh
            {
                Radius = 0.05f,
                Height = 0.05f,
                RadialSegments = 6,
                Rings = 3
            };
        }
        // HANDS / FEET
        else if (name.Contains("hand") || name.Contains("foot"))
        {
            mesh = new BoxMesh
            {
                Size = new Vector3(0.1f, 0.05f, 0.1f)
            };
        }
        // FINGERS / THUMBS
        else if (name.Contains("finger") || name.Contains("thumb"))
        {
            mesh = new CapsuleMesh
            {
                Radius = 0.025f,
                Height = 0.07f,
                RadialSegments = 6,
                Rings = 2
            };
        }
        // Fallback
        else
        {
            mesh = new BoxMesh
            {
                Size = new Vector3(0.1f, 0.1f, 0.1f)
            };
        }

        meshInstance.Mesh = mesh;

        var material = new StandardMaterial3D
        {
            AlbedoColor = GetColorForJoint(name),
            Roughness = 0.9f
        };

        meshInstance.MaterialOverride = material;
        AddChild(meshInstance);
        _jointVisuals[joint] = meshInstance;
    }
}

    
private Color GetColorForJoint(string name)
{
    if (name.Contains("head") || name.Contains("hand") || name.Contains("finger") || name.Contains("thumb"))
        return new Color(1f, 0.8f, 0.6f); // Skin tone

    if (name.Contains("torso")) return new Color(0.2f, 0.6f, 1f);         // Shirt
    if (name.Contains("leg") || name.Contains("hip") || name.Contains("knee")) return new Color(0.1f, 0.1f, 0.1f); // Pants
    if (name.Contains("foot") || name.Contains("ankle")) return new Color(0.1f, 0.1f, 0.1f); // Shoes
    if (name.Contains("arm") || name.Contains("shoulder") || name.Contains("elbow")) return new Color(1f, 0.8f, 0.6f); // Arms

    return new Color(1, 1, 1); // fallback white
}




    private void UpdateVisualJoints()
    {
        foreach (var (joint, visual) in _jointVisuals)
        {
            visual.GlobalTransform = joint.GlobalTransform;
        }
    }
    
    private void DrawBoneLines()
    {
        _boneLines.ClearSurfaces();

        _boneLines.SurfaceBegin(Mesh.PrimitiveType.Lines);

        foreach (var joint in _skeleton.AllJoints)
        {
            if (joint.Parent != null)
            {
                var start = joint.Parent.GlobalTransform.Origin;
                var end = joint.GlobalTransform.Origin;

                _boneLines.SurfaceAddVertex(start);
                _boneLines.SurfaceAddVertex(end);
            }
        }

        _boneLines.SurfaceEnd();
    }
    
    private void DrawJointRecursive(Joint joint)
    {
        ImGui.PushID(joint.Name); // Prevent ID collisions

        if (ImGui.TreeNode(joint.Name))
        {
            if (ImGui.TreeNode("Transform"))
            {
                var pos = joint.LocalPosition.ToNumerics();
                var rot = joint.LocalRotation.ToNumerics();
                var scale = joint.LocalScale.ToNumerics();

                if (ImGui.DragFloat3("Position", ref pos, 0.01f))
                    joint.LocalPosition = pos.ToGodot();

                if (ImGui.DragFloat3("Rotation", ref rot, 0.5f))
                    joint.LocalRotation = rot.ToGodot();

                if (ImGui.DragFloat3("Scale", ref scale, 0.01f))
                    joint.LocalScale = scale.ToGodot();

                ImGui.TreePop();
            }

            if (joint.Children.Count > 0)
            {
                if (ImGui.TreeNode("Children"))
                {
                    foreach (var child in joint.Children)
                        DrawJointRecursive(child);
                    ImGui.TreePop();
                }
            }

            ImGui.TreePop();
        }

        ImGui.PopID();
    }




    public override void _PhysicsProcess(double delta)
    {
      
    }

}