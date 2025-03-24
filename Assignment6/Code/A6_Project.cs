using Godot;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

public partial class A6_Project : Node3D
{
    private Skeleton _skeleton;
    private Dictionary<Joint, MeshInstance3D> _jointVisuals = new();
    
    private ImmediateMesh _boneLines;
    private MeshInstance3D _boneRenderer;

    private const float BaseWidth = 1280f;
    private const float BaseHeight = 720f;
    private const float BaseScale = 1.0f;

    [Export] private ShaderMaterial _stylizedMaterial;
   public override void _Ready()
    {
        _skeleton = new Skeleton();

        // Core body proportions (stylized)
        var torso = _skeleton.Root;
        torso.Name = "Torso";
        torso.LocalPosition = new Vector3(0, 0, 0);

        var neck = _skeleton.CreateJoint("Neck", torso);
        neck.LocalPosition = new Vector3(0, 0.25f, 0);

        var head = _skeleton.CreateJoint("Head", neck);
        head.LocalPosition = new Vector3(0, 0.45f, 0);

        // Left Arm
        var lShoulder = _skeleton.CreateJoint("LeftShoulder", torso);
        lShoulder.LocalPosition = new Vector3(-0.2f, 0.2f, 0);

        var lElbow = _skeleton.CreateJoint("LeftElbow", lShoulder);
        lElbow.LocalPosition = new Vector3(-0.2f, 0, 0);

        var lWrist = _skeleton.CreateJoint("LeftWrist", lElbow);
        lWrist.LocalPosition = new Vector3(-0.2f, 0, 0);

        var lHand = _skeleton.CreateJoint("LeftHand", lWrist);
        lHand.LocalPosition = new Vector3(-0.1f, 0, 0);

        // Right Arm
        var rShoulder = _skeleton.CreateJoint("RightShoulder", torso);
        rShoulder.LocalPosition = new Vector3(0.2f, 0.2f, 0);

        var rElbow = _skeleton.CreateJoint("RightElbow", rShoulder);
        rElbow.LocalPosition = new Vector3(0.2f, 0, 0);

        var rWrist = _skeleton.CreateJoint("RightWrist", rElbow);
        rWrist.LocalPosition = new Vector3(0.2f, 0, 0);

        var rHand = _skeleton.CreateJoint("RightHand", rWrist);
        rHand.LocalPosition = new Vector3(0.1f, 0, 0);

        // Left Leg
        var lHip = _skeleton.CreateJoint("LeftHip", torso);
        lHip.LocalPosition = new Vector3(-0.12f, -0.25f, 0);

        var lKnee = _skeleton.CreateJoint("LeftKnee", lHip);
        lKnee.LocalPosition = new Vector3(0, -0.25f, 0);

        var lAnkle = _skeleton.CreateJoint("LeftAnkle", lKnee);
        lAnkle.LocalPosition = new Vector3(0, -0.15f, 0);

        var lFoot = _skeleton.CreateJoint("LeftFoot", lAnkle);
        lFoot.LocalPosition = new Vector3(0, -0.05f, 0.1f);

        // Right Leg
        var rHip = _skeleton.CreateJoint("RightHip", torso);
        rHip.LocalPosition = new Vector3(0.12f, -0.25f, 0);

        var rKnee = _skeleton.CreateJoint("RightKnee", rHip);
        rKnee.LocalPosition = new Vector3(0, -0.25f, 0);

        var rAnkle = _skeleton.CreateJoint("RightAnkle", rKnee);
        rAnkle.LocalPosition = new Vector3(0, -0.15f, 0);

        var rFoot = _skeleton.CreateJoint("RightFoot", rAnkle);
        rFoot.LocalPosition = new Vector3(0, -0.05f, 0.1f);

        ApplyAPose();
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
        UpdateImGuiScale();

        if (ImGui.Begin("Skeleton Joints"))
        {
            DrawJointRecursive(_skeleton.Root);
           
        }
        ImGui.End();
    }



    
  private void CreateVisualJoints()
    {
        foreach (var joint in _skeleton.AllJoints)
        {
            var meshInstance = new MeshInstance3D();
            PrimitiveMesh mesh;
            string name = joint.Name.ToLowerInvariant();

            if (name.Contains("head"))
            {
                mesh = new SphereMesh 
                {
                        Radius = 0.15f,
                        Height = 0.25f,
                        RadialSegments = 12,
                        Rings = 6
                };
            }
            else if (name.Contains("neck"))
            {
                mesh = new CylinderMesh { TopRadius = 0.06f, BottomRadius = 0.06f, Height = 0.1f, RadialSegments = 6 };
            }
            else if (name.Contains("torso"))
            {
                mesh = new BoxMesh { Size = new Vector3(0.18f, 0.3f, 0.1f) };
            }
            else if (name.Contains("shoulder") || name.Contains("elbow") || name.Contains("knee"))
            {
                mesh = new SphereMesh { Radius = 0.08f, Height = 0.08f, RadialSegments = 6, Rings = 3 };
            }
            else if (name.Contains("arm") || name.Contains("leg") || name.Contains("hip"))
            {
                mesh = new CapsuleMesh { Radius = 0.075f, Height = 0.2f, RadialSegments = 6, Rings = 3 };
            }
            else if (name.Contains("wrist") || name.Contains("ankle"))
            {
                mesh = new SphereMesh { Radius = 0.06f, Height = 0.06f, RadialSegments = 6, Rings = 3 };
            }
            else if (name.Contains("hand"))
            {
                mesh = new BoxMesh { Size = new Vector3(0.1f, 0.05f, 0.1f) };
            }
            else if (name.Contains("foot"))
            {
                mesh = new BoxMesh { Size = new Vector3(0.12f, 0.05f, 0.15f) };
            }
            else
            {
                mesh = new BoxMesh { Size = new Vector3(0.1f, 0.1f, 0.1f) };
            }

            meshInstance.Mesh = mesh;
            var material = new StandardMaterial3D
            {
                AlbedoColor = GetColorForJoint(name),
                Roughness = 0.9f
            };
            
            meshInstance.Mesh = mesh;

            var instanceMaterial = (ShaderMaterial)_stylizedMaterial.Duplicate();
            instanceMaterial.SetShaderParameter("albedo_color", GetColorForJoint(name));
            meshInstance.MaterialOverride = instanceMaterial;
            meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;

            AddChild(meshInstance);
            _jointVisuals[joint] = meshInstance;
        }
    }

    private Color GetColorForJoint(string name)
    {
        if (name.Contains("head") || name.Contains("hand")) return new Color(1f, 0.8f, 0.6f);
        if (name.Contains("torso")) return new Color(0.2f, 0.6f, 1f);
        if (name.Contains("leg") || name.Contains("hip") || name.Contains("knee")) return new Color(0.1f, 0.1f, 0.1f);
        if (name.Contains("foot") || name.Contains("ankle")) return new Color(0.1f, 0.1f, 0.1f);
        if (name.Contains("arm") || name.Contains("shoulder") || name.Contains("elbow")) return new Color(1f, 0.8f, 0.6f);
        return new Color(1, 1, 1);
    }


private Vector2 _lastViewportSize = Vector2.Zero;

private void UpdateImGuiScale()
{
    var size = GetViewport().GetVisibleRect().Size;

    if (size == _lastViewportSize)
        return;

    _lastViewportSize = size;

    float scaleX = size.X / BaseWidth;
    float scaleY = size.Y / BaseHeight;
    float uiScale = Mathf.Clamp(Mathf.Min(scaleX, scaleY), 0.6f, 2.0f);

    ImGui.GetIO().FontGlobalScale = uiScale;
    ImGui.GetStyle().ScaleAllSizes(uiScale);
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
        _boneLines.SurfaceBegin(Mesh.PrimitiveType.Triangles);

        foreach (var joint in _skeleton.AllJoints)
        {
            if (joint.Parent == null) continue;

            var start = joint.Parent.GlobalTransform.Origin;
            var end = joint.GlobalTransform.Origin;

            var dir = (end - start).Normalized();
            var length = (end - start).Length();
            var up = Vector3.Up;

            if (Mathf.Abs(dir.Dot(up)) > 0.99f)
                up = Vector3.Right; // avoid gimbal lock

            var right = dir.Cross(up).Normalized();
            up = right.Cross(dir).Normalized();

            float halfWidth = 0.02f;

            // Build 8 corners of the prism
            Vector3 offset1 = right * halfWidth + up * halfWidth;
            Vector3 offset2 = right * halfWidth - up * halfWidth;
            Vector3 offset3 = -right * halfWidth + up * halfWidth;
            Vector3 offset4 = -right * halfWidth - up * halfWidth;

            Vector3 p0 = start + offset1;
            Vector3 p1 = start + offset2;
            Vector3 p2 = start + offset3;
            Vector3 p3 = start + offset4;

            Vector3 p4 = end + offset1;
            Vector3 p5 = end + offset2;
            Vector3 p6 = end + offset3;
            Vector3 p7 = end + offset4;

            // Front face
            AddQuad(p0, p1, p5, p4);
            // Back face
            AddQuad(p3, p2, p6, p7);
            // Top face
            AddQuad(p2, p0, p4, p6);
            // Bottom face
            AddQuad(p1, p3, p7, p5);
            // Left face
            AddQuad(p2, p3, p1, p0);
            // Right face
            AddQuad(p4, p5, p7, p6);
        }

        _boneLines.SurfaceEnd();
    }

    private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        _boneLines.SurfaceAddVertex(a);
        _boneLines.SurfaceAddVertex(b);
        _boneLines.SurfaceAddVertex(c);

        _boneLines.SurfaceAddVertex(a);
        _boneLines.SurfaceAddVertex(c);
        _boneLines.SurfaceAddVertex(d);
    }


    
    private void DrawJointRecursive(Joint joint)
    {
        ImGui.PushID(joint.Name);

        bool open = ImGui.TreeNodeEx(joint.Name, ImGuiTreeNodeFlags.DefaultOpen);

        if (open)
        {
            var pos = joint.LocalPosition.ToNumerics();
            var rot = joint.LocalRotation.ToNumerics();
            var scale = joint.LocalScale.ToNumerics();

            if (ImGui.DragFloat3("Position##" + joint.Name, ref pos, 0.01f))
                joint.LocalPosition = pos.ToGodot();

            if (ImGui.DragFloat3("Rotation##" + joint.Name, ref rot, 0.5f))
                joint.LocalRotation = rot.ToGodot();

            if (ImGui.DragFloat3("Scale##" + joint.Name, ref scale, 0.01f))
                joint.LocalScale = scale.ToGodot();


            if (joint.Children.Count > 0 && ImGui.TreeNode("Children##" + joint.Name))
            {
                foreach (var child in joint.Children)
                    DrawJointRecursive(child);
                ImGui.TreePop();
            }

            ImGui.TreePop();
        }

        ImGui.PopID();
    }

    private void ApplyAPose()
    {
        // Position Adjustments (from your notes)
        _skeleton.Find("Head")!.LocalPosition = new Vector3(0, 0.14f, 0);
        _skeleton.Find("Neck")!.LocalPosition = new Vector3(0, 0.220f, 0);
        _skeleton.Find("LeftShoulder")!.LocalPosition = new Vector3(-0.15f, 0.16f, 0);
        _skeleton.Find("RightShoulder")!.LocalPosition = new Vector3(0.15f, 0.16f, 0);

        // Left Arm Rotations
        _skeleton.Find("LeftShoulder")!.LocalRotation = new Vector3(0, 0, 25);
        _skeleton.Find("LeftElbow")!.LocalRotation = new Vector3(0, 0, 10);
        _skeleton.Find("LeftWrist")!.LocalRotation = new Vector3(0, 0, 5);
        _skeleton.Find("LeftHand")!.LocalRotation = new Vector3(0, 0, 5);

        // Right Arm Rotations
        _skeleton.Find("RightShoulder")!.LocalRotation = new Vector3(0, 0, -25);
        _skeleton.Find("RightElbow")!.LocalRotation = new Vector3(0, 0, -10);
        _skeleton.Find("RightWrist")!.LocalRotation = new Vector3(0, 0, -5);
        _skeleton.Find("RightHand")!.LocalRotation = new Vector3(0, 0, -5);
    }




    public override void _PhysicsProcess(double delta)
    {
      
    }

}