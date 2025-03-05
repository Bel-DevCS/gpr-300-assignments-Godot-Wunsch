using Godot;
using System;

public partial class A4_SwapParticle : GpuParticles3D
{
    private ParticleProcessMaterial particleMaterial;

    public override void _Ready()
    {
        InitializeParticleEffect();
    }

    public override void _Process(double delta)
    {
      //  if(Input.IsActionJustPressed("right_arrow")) TriggerEffect(new Vector3(0,0,0));
    }

    private void InitializeParticleEffect()
    {
        ProcessMode = GpuParticles3D.ProcessModeEnum.Always; // Always update particles
        OneShot = true;
        Amount = 300;
        Lifetime = 1f;
        Explosiveness = 1.0f;
        
        
        particleMaterial = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 1.5f,
            InitialVelocityMin = 2.0f,
            InitialVelocityMax = 5.0f,
            Direction = new Vector3(0, 1, 0),
            Spread = 20,
            Gravity = new Vector3(0, -5, 0),
            Color = new Color(0.0f, 1f, 1f),
            
            ScaleMin = 0.01f,
            ScaleMax = 0.25f,
        };

        ProcessMaterial = particleMaterial;
    }

    public void TriggerEffect(Vector3 position)
    {
        GD.Print("Triggering Swap Particle");
        GlobalTransform = new Transform3D(Basis.Identity, position);
        Emitting = true;
    }
}