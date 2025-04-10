using Godot;
using System;
using ImGuiNET;

public partial class Testing : Node
{
    private GameletEditorUI _gameletEditorUI;
    
    public override void _Ready()
    {
        _gameletEditorUI = new GameletEditorUI();
    }
    
    public override void _Process(double delta)
    {
        DrawImGui();
    }

    void DrawImGui()
    {
        _gameletEditorUI?.Draw();
    }

    void DrawCompass()
    {
        var viewport = ImGui.GetMainViewport();
        var screenWidth = viewport.Size.X;

        var position = new System.Numerics.Vector2(screenWidth * 0.5f, 20);
        ImGui.SetNextWindowPos(position, ImGuiCond.Always, new System.Numerics.Vector2(0.5f, 0));

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 0));
        ImGui.PushStyleColor(ImGuiCol.TitleBg, new System.Numerics.Vector4(0, 0, 0, 0));
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new System.Numerics.Vector4(0, 0, 0, 0));

        if (ImGui.Begin("Compass", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoInputs))
        {
            ImGui.Text("Compass Info");
        }
        ImGui.End();

        ImGui.PopStyleColor(3);
    }
    
}
