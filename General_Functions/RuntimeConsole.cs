using System;
using System.Collections.Generic;
using ImGuiNET;
using Godot;
using System.Numerics;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

public static class RuntimeConsole
{
    private static readonly List<(string message, LogType type)> _logs = new();
    private static bool _showConsole = true;
    private static bool _autoScroll = true;
    private static Vector2 _consoleSize = new Vector2(600, 300);
    
    private static string _inputBuffer = "";
    private static bool _isTyping = false;
    public static bool IsTyping => _isTyping;

    private enum LogType
    {
        Info,
        System,
        Error,
        Admin
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
    
    static void LogAdmin(string message)
    {
        _logs.Add(("<Admin> " + message, LogType.Admin));
        GD.Print(message);
    }

    public static void Toggle() => _showConsole = !_showConsole;

    public static void ClearLog() => _logs.Clear();

    public static void Draw()
    {
        if (!_showConsole) return;

        Godot.Vector2 windowSize = GetWindowSize();
        float scaleFactor = windowSize.Y / 1080f; // Adjust scaling based on vertical resolution (e.g., 1080p as base)

        // Scale ImGui font and UI
        ImGui.GetIO().FontGlobalScale = scaleFactor;

        // Set size (40% of screen) and bottom-right anchored position
        Vector2 scaledSize = (windowSize * 0.4f).ToNumerics();
        Vector2 windowPos = new Vector2(windowSize.X, windowSize.Y) - scaledSize - new Vector2(10, 10); // 10px margin

        ImGui.SetNextWindowSize(scaledSize, ImGuiCond.Always);
        ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);

        if (ImGui.Begin("Runtime Console", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
        {
            if (ImGui.Button("Clear"))
                _logs.Clear();

            ImGui.Separator();

            ImGui.BeginChild("LogRegion", new Vector2(0, -30), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

            foreach (var (message, type) in _logs)
            {
                bool pushedColor = false;

                switch (type)
                {
                    case LogType.Info:
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 0.8f, 1f)); pushedColor = true; break;
                    case LogType.Error:
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.3f, 0.3f, 1f)); pushedColor = true; break;
                    case LogType.System:
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 0.8f, 1f, 1f)); pushedColor = true; break;
                    case LogType.Admin:
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 0.3f, 1f)); pushedColor = true; break;
                }

                ImGui.TextUnformatted(message);

                if (pushedColor) ImGui.PopStyleColor();
            }

            if (_autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1f);
            }

            ImGui.EndChild();
        }

        ImGui.Separator();
        ImGui.InputText("##ConsoleInput", ref _inputBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue);
        _isTyping = ImGui.IsItemActive(); 
        ImGui.SameLine();
        if (ImGui.Button("Send") || ImGui.IsKeyPressed(ImGuiKey.Enter))
        {
            HandleCommand(_inputBuffer.Trim());
            _inputBuffer = "";
        }
        
        ImGui.End();
    }


    public static Vector2 ToNumerics(this Godot.Vector2 vec)
    {
        return new Vector2(vec.X, vec.Y);
    }
    
    private static Godot.Vector2 GetWindowSize()
    {
        return DisplayServer.WindowGetSize();
    }
    
    private static void HandleCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        if (input.StartsWith("/"))
        {
            switch (input.ToLowerInvariant())
            {
                case "/endprogram":
                    LogMessage("<Admin> has terminated program.");
                    GetTree()?.Quit();
                    break;

                default:
                    LogError($"Unknown command: {input}");
                    break;
            }
        }
        else
        {
            LogAdmin(input);
        }
    }

    private static SceneTree GetTree()
    {
        return Engine.IsEditorHint() ? null : (SceneTree)Engine.GetMainLoop();
    }


}