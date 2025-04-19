using Godot;
using System;

public partial class PointNode : Node3D
{
    public string Label { get; set; } = "Point";
    public Vector3 Scale = Vector3.One;
    public Color DebugColor = new Color(1, 0, 0);
    public Shape ParentShape { get; set; } = null;

    private MeshInstance3D _debugSphere;

    public override void _Ready()
    {
        CreateDebugSphere();
    }

    private void CreateDebugSphere()
    {
        _debugSphere = new MeshInstance3D
        {
            Mesh = new SphereMesh
            {
                Radius = 0.1f,
                Height = 0.2f,
                RadialSegments = 8,
                Rings = 8
            },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = DebugColor,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            }
        };

        var colliderBody = new StaticBody3D();
        var colliderShape = new CollisionShape3D
        {
            Shape = new SphereShape3D { Radius = 0.1f }
        };

        colliderBody.AddChild(colliderShape);
        _debugSphere.AddChild(colliderBody);
        AddChild(_debugSphere);
    }

    public Transform3D ToTransform()
    {
        return new Transform3D(Basis.Identity.Scaled(Scale), GlobalTransform.Origin);
    }

    public void SetColor(Color color)
    {
        DebugColor = color;
        if (_debugSphere != null && _debugSphere.MaterialOverride is StandardMaterial3D mat)
            mat.AlbedoColor = color;
    }
    
    public bool ShowDebug
    {
        get => _debugSphere?.Visible ?? false;
        set
        {
            if (_debugSphere != null)
                _debugSphere.Visible = value;
        }
    }

}