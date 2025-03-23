using System.Numerics;
using Godot;
using Vector3 = Godot.Vector3;

public static class VectorExtensions
{
    public static Vector3 ToGodot(this System.Numerics.Vector3 v)
        => new Vector3(v.X, v.Y, v.Z);

    public static System.Numerics.Vector3 ToNumerics(this Vector3 v)
        => new System.Numerics.Vector3(v.X, v.Y, v.Z);
}