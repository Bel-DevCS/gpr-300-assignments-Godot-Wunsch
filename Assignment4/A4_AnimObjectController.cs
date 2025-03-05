using Godot;
using System;

public partial class A4_AnimObjectController : Node
{
    [Export] public Node ObjectNode;
    private int materialIndex = 0;

    public override void _Process(double delta)
    {
        if (ObjectNode is A4_ObjectSwitcher objSwitcher)
        {
            // Switch Models
            if (Input.IsActionJustPressed("right_arrow"))
            {
                objSwitcher.NextModel();
                materialIndex = 0;
            }
            
            if (Input.IsActionJustPressed("left_arrow"))
            {
                    objSwitcher.LastModel();
                    materialIndex = 0;
            }

            // Cycle Through Materials
            if (Input.IsActionJustPressed("select"))
            {
                int materialCount = objSwitcher.GetAllMaterials().Count;
                if (materialCount > 0)
                {
                    materialIndex = (materialIndex + 1) % materialCount;
                    GD.Print($"Selected Material Index: {materialIndex}");
                }
                else
                {
                    GD.PrintErr("No materials found!");
                }
            }

            // Change Selected Material's Color
            if (Input.IsActionJustPressed("up_arrow"))
            {
                objSwitcher.ChangeMaterialColor(materialIndex, Colors.White);
                GD.Print($"Material {materialIndex} Set to White");
            }

            if (Input.IsActionJustPressed("down_arrow"))
            {
                objSwitcher.ChangeMaterialColor(materialIndex, Colors.Cyan);
                GD.Print($"Material {materialIndex} Set to Cyan");
            }
        }
    }
}