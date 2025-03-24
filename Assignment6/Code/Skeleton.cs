using Godot;
using System.Collections.Generic;

public class Skeleton
{
    public Joint Root { get; private set; }
    public List<Joint> AllJoints { get; private set; } = new();

    public Skeleton()
    {
        Root = CreateJoint("Torso", null);
        Root.LocalPosition = new Vector3(0, 0, 0);

        /*
        var shoulder = CreateJoint("Shoulder", Root);
        shoulder.LocalPosition = new Vector3(0, 0.5f, 0);

        var elbow = CreateJoint("Elbow", shoulder);
        elbow.LocalPosition = new Vector3(0, 0.5f, 0);

        var wrist = CreateJoint("Wrist", elbow);
        wrist.LocalPosition = new Vector3(0, 0.5f, 0);
        */
    }


    public Joint CreateJoint(string name, Joint parent)
    {
        var joint = new Joint(name, parent);
        AllJoints.Add(joint);
        return joint;
    }

    public void Update()
    {
        Root?.UpdateGlobalTransform();
    }
}