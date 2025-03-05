using Godot;
using System;

public class KeyframeData
{
    public float Time;
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scale;
    public Color MaterialColor;
    public string MeshName;

    public KeyframeData(float time, Vector3 position, Vector3 rotation, Vector3 scale, Color materialColor, string meshName)
    {
        Time = time;
        Position = position;
        Rotation = rotation;
        Scale = scale;
        MaterialColor = materialColor;
        MeshName = meshName;
    }
}