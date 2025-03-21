using System;
using System.Collections.Generic;
using ImGuiNET;
using Godot;

public static class RuntimeConsole
{
    private static readonly List<(string message, LogType type)> _logs = new();
    private static bool _showConsole = true;
    private static bool _autoScroll = true;
    private static Vector2 _consoleSize = new Vector2(600, 300);

    private enum LogType
    {
        Info,
        System,
        Error
    }

    public static void Log(string message)
    {
        _logs.Add((message, LogType.Info));
        GD.Print(message);
    }

    public static void LogMessage(string message)
    {
        _logs.Add(("<System> " + message, LogType.System));
        GD.Print(message);
    }

    public static void LogError(string message)
    {
        _logs.Add(("<Error> " + message, LogType.Error));
        GD.PrintErr(message);
    }

    public static void Toggle() => _showConsole = !_showConsole;

    public static void ClearLog() => _logs.Clear();

    public static void Draw()
    {
        if (!_showConsole) return;

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(_consoleSize.X, _consoleSize.Y), ImGuiCond.FirstUseEver);
        ImGui.Begin("Runtime Console", ImGuiWindowFlags.NoCollapse);

        // Options
        ImGui.Checkbox("Auto Scroll", ref _autoScroll);
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
            ClearLog();

        ImGui.Separator();

        // Log content
        ImGui.BeginChild("ConsoleScroll", new System.Numerics.Vector2(0, -ImGui.GetFrameHeightWithSpacing()), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

        foreach (var (msg, type) in _logs)
        {
            switch (type)
            {
                case LogType.System:
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.5f, 1f, 0.5f, 1f)); // Green
                    break;
                case LogType.Error:
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1f, 0.4f, 0.4f, 1f)); // Red
                    break;
                default:
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1f, 1f, 1f, 1f)); // White
                    break;
            }

            ImGui.TextUnformatted(msg);
            ImGui.PopStyleColor();
        }

        // Auto-scroll
        if (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            ImGui.SetScrollHereY(1f);

        ImGui.EndChild();
        ImGui.End();
    }
}
