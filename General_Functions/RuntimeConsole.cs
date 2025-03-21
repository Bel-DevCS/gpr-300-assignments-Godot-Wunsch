using System.Collections.Generic;
using ImGuiNET;
using Godot;

public static class RuntimeConsole
{
    private static readonly List<string> _logs = new();
    private static bool _showConsole = true;

    public static void Log(string message)
    {
        _logs.Add(message);
        GD.Print(message);
    }

    public static void Toggle()
    {
        _showConsole = !_showConsole;
    }

    public static void Draw()
    {
        if (!_showConsole) return;

        ImGui.Begin("Runtime Console");

        foreach (string log in _logs)
        {
            ImGui.TextUnformatted(log);
        }

        if (ImGui.Button("Clear"))
        {
            _logs.Clear();
        }

        ImGui.End();
    }
}