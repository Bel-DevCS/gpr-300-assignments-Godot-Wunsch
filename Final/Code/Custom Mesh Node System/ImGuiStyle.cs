using System;
using Godot;
using ImGuiNET;

public static class ImGuiStyle
{
  public static void ApplyDarkModelingTheme()
{
    var style = ImGui.GetStyle();
    var colors = style.Colors;

    ImGui.StyleColorsDark(); // Base

    style.WindowRounding = 4f;
    style.FrameRounding = 3f;
    style.GrabRounding = 2f;
    style.TabRounding = 3f;
    style.ScrollbarRounding = 3f;

    style.WindowBorderSize = 1f;
    style.FrameBorderSize = 1f;
    style.TabBorderSize = 1f;

    style.WindowPadding = new System.Numerics.Vector2(8, 6);
    style.ItemSpacing = new System.Numerics.Vector2(8, 6);
    style.FramePadding = new System.Numerics.Vector2(6, 4);
    style.GrabMinSize = 16f;

    var bg     = new System.Numerics.Vector4(0.11f, 0.10f, 0.12f, 1.0f);  // deep charcoal
    var accent = new System.Numerics.Vector4(0.67f, 0.45f, 0.75f, 1.0f);  // soft lavender
    var hover  = new System.Numerics.Vector4(0.85f, 0.55f, 0.85f, 1.0f);  // magenta-ish
    var active = new System.Numerics.Vector4(1.0f, 0.65f, 0.95f, 1.0f);  // highlight

    colors[(int)ImGuiCol.Header]         = accent;
    colors[(int)ImGuiCol.HeaderHovered]  = hover;
    colors[(int)ImGuiCol.HeaderActive]   = active;

    colors[(int)ImGuiCol.Button]         = accent;
    colors[(int)ImGuiCol.ButtonHovered]  = hover;
    colors[(int)ImGuiCol.ButtonActive]   = active;

    colors[(int)ImGuiCol.Tab]            = accent;
    colors[(int)ImGuiCol.TabHovered]     = hover;
    colors[(int)ImGuiCol.TabSelected]    = active;

    colors[(int)ImGuiCol.FrameBg]        = bg;
    colors[(int)ImGuiCol.FrameBgHovered] = accent * 0.4f;
    colors[(int)ImGuiCol.FrameBgActive]  = accent * 0.6f;

    colors[(int)ImGuiCol.WindowBg]       = bg;
    colors[(int)ImGuiCol.PopupBg]        = bg;
    colors[(int)ImGuiCol.ChildBg]        = bg;

    colors[(int)ImGuiCol.Border]         = new System.Numerics.Vector4(0.25f, 0.2f, 0.3f, 1.0f);
    colors[(int)ImGuiCol.BorderShadow]   = new System.Numerics.Vector4(0, 0, 0, 0);

    colors[(int)ImGuiCol.Text]           = new System.Numerics.Vector4(0.95f, 0.95f, 0.98f, 1f);
    colors[(int)ImGuiCol.TextDisabled]   = new System.Numerics.Vector4(0.5f, 0.5f, 0.55f, 1f);
    
   
    colors[(int)ImGuiCol.TitleBg] = bg * 1.2f;
    colors[(int)ImGuiCol.TitleBgActive] = accent * 0.5f;
    colors[(int)ImGuiCol.TitleBgCollapsed] = bg * 0.9f;
    
    colors[(int)ImGuiCol.WindowBg] = new System.Numerics.Vector4(0.14f, 0.13f, 0.16f, 1.0f); // Slightly warmer charcoal
    colors[(int)ImGuiCol.ChildBg] = colors[(int)ImGuiCol.WindowBg];
    colors[(int)ImGuiCol.PopupBg] = colors[(int)ImGuiCol.WindowBg];

}

}