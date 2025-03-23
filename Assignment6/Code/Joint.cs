using Godot;
using System;
using System.Collections.Generic;

public class Joint
{
    public string Name;
    public Vector3 LocalPosition = Vector3.Zero;
    public Vector3 LocalRotation = Vector3.Zero; // Euler angles in degrees
    public Vector3 LocalScale = Vector3.One;

    public Transform3D GlobalTransform = Transform3D.Identity;

    public Joint Parent = null;
    public List<Joint> Children = new();

    public Joint(string name, Joint parent = null)
    {
        Name = name;
        Parent = parent;
        Parent?.Children.Add(this);
    }

    public void UpdateGlobalTransform()
    {
        var localTransform = BuildLocalTransform();

        GlobalTransform = Parent != null
            ? Parent.GlobalTransform * localTransform
            : localTransform;

        foreach (var child in Children)
            child.UpdateGlobalTransform();
    }

    private Transform3D BuildLocalTransform()
    {
      
        Vector3 eulerInRadians = new Vector3(
            Mathf.DegToRad(LocalRotation.X),
            Mathf.DegToRad(LocalRotation.Y),
            Mathf.DegToRad(LocalRotation.Z)
        );

        Basis basis = Basis.FromEuler(eulerInRadians);
        basis = basis.Scaled(LocalScale); 

        return new Transform3D(basis, LocalPosition);
    }

}