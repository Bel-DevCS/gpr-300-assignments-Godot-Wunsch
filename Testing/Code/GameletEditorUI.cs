using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ImGuiNET;

using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

public class GameletEditorUI
{
    private List<string> gameletNames = new() { "TestGamelet", "EnemyPack01", "BossFight" };
    private int selectedIndex = 0;
    private GameletData editingData = new();
    private string newGameletName = "";

public void Draw()
{
    var viewport = ImGui.GetMainViewport();

    // === Scaling ===
    float baseScale = viewport.Size.Y / 1080.0f;
    float userScale = 1.5f;
    float finalScale = baseScale * userScale;
    ImGui.GetIO().FontGlobalScale = finalScale;

    // === Layout Metrics ===
    float sidebarWidth = 300f * finalScale;
    float footerHeight = 80f * finalScale;
    float buttonWidth = 120f * finalScale;
    float buttonSpacing = 8f * finalScale;

    var style = ImGui.GetStyle();
    style.WindowPadding = new Vector2(8, 8) * finalScale;
    style.FramePadding = new Vector2(6, 4) * finalScale;
    style.ItemSpacing = new Vector2(8, 6) * finalScale;
    style.FrameRounding = 0.0f;

    // === Style Colors ===
    ImGui.StyleColorsDark(); // Reset
    ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0.85f));
    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));
    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 1));
    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1.0f, 0.8f, 0.0f, 1));
    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1.0f, 0.7f, 0.0f, 1));
    ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.2f, 0.2f, 0.2f, 1));
    ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.3f, 0.3f, 0.3f, 1));
    ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.4f, 0.4f, 0.4f, 1));
    ImGui.PushStyleColor(ImGuiCol.SliderGrab, new Vector4(0.8f, 0.6f, 0.0f, 1));
    ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(1.0f, 0.8f, 0.0f, 1));
    ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.8f, 0.6f, 0.0f, 0.25f));
    ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(1.0f, 0.8f, 0.0f, 0.25f));
    ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(1.0f, 0.7f, 0.0f, 0.25f));

    // === Window ===
    var flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar;
    ImGui.SetNextWindowPos(Vector2.Zero);
    ImGui.SetNextWindowSize(viewport.Size);
    ImGui.Begin("Gamelet Editor", flags);

    // === Columns ===
    ImGui.Columns(2);
    ImGui.SetColumnWidth(0, sidebarWidth);

    // === Left Panel ===
    ImGui.BeginChild("GameletList", new Vector2(0, viewport.Size.Y - footerHeight));
    ImGui.Text("Gamelets:");
    for (int i = 0; i < gameletNames.Count; i++)
    {
        bool selected = (i == selectedIndex);
        ImGui.PushStyleColor(ImGuiCol.Text, selected ? new Vector4(1f, 0.8f, 0.0f, 1f) : new Vector4(1f, 1f, 1f, 1f));

        if (ImGui.Selectable(gameletNames[i], selected))
        {
            selectedIndex = i;
            LoadGamelet(gameletNames[i]);
        }

        ImGui.PopStyleColor();
    }
    ImGui.EndChild();

    ImGui.NextColumn();

    // === Right Panel ===
    ImGui.BeginChild("Editor", new Vector2(0, viewport.Size.Y - footerHeight));
    ImGui.Text("Edit Gamelet Data:");

    ImGui.InputInt("Starting Points", ref editingData.startingPoints);
    ImGui.InputInt("Point Gain", ref editingData.pointGain);
    ImGui.InputInt("Point Loss", ref editingData.pointLoss);
    ImGui.InputFloat("Base Radius", ref editingData.baseRadius);
    ImGui.InputFloat("Radius Growth", ref editingData.radiusGrowth);
    ImGui.InputFloat("Spawn Count Interval", ref editingData.spawnCountInterval);
    ImGui.InputInt("Max Spawn Count", ref editingData.maxSpawnCount);
    ImGui.InputFloat("Max Spawn Interval", ref editingData.maxSpawnInterval);
    ImGui.InputFloat("Min Spawn Interval", ref editingData.minSpawnInterval);
    ImGui.InputFloat("Interval Reduction", ref editingData.intervalReduction);


    ImGui.EndChild();

    // === Footer ===
    ImGui.Columns(1);
    ImGui.Separator();
    ImGui.BeginChild("ButtonBar", new Vector2(0, footerHeight), ImGuiChildFlags.Borders);

    float buttonHeight = style.FramePadding.Y * 2 + ImGui.GetTextLineHeight();

    // === Back Button ===
    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 1));
    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.5f, 0.5f, 1));
    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.4f, 0.4f, 1));

    if (ImGui.Button("Back", new Vector2(buttonWidth, buttonHeight)))
        GD.Print("Back clicked");

    ImGui.PopStyleColor(3);

    // === Centered Buttons ===
    float totalWidth = 3 * buttonWidth + 2 * buttonSpacing;
    float available = viewport.Size.X - style.WindowPadding.X * 2;
    ImGui.SetCursorPosX((available - totalWidth) * 0.5f);

    if (ImGui.Button("Create New [C]", new Vector2(buttonWidth, buttonHeight)))
        ImGui.OpenPopup("CreateGamelet");

    ImGui.SameLine(0, buttonSpacing);

    if (ImGui.Button("Save [S]", new Vector2(buttonWidth, buttonHeight)))
        SaveGamelet();

    ImGui.SameLine(0, buttonSpacing);

    if (ImGui.Button("Delete [D]", new Vector2(buttonWidth, buttonHeight)))
        ImGui.OpenPopup("ConfirmDelete");

    ImGui.EndChild();

    // === Popups ===
    var center = ImGui.GetMainViewport().GetCenter();

// --- CreateGamelet ---
    ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
    if (ImGui.BeginPopupModal("CreateGamelet", ImGuiWindowFlags.AlwaysAutoResize))
    {
        ImGui.Text("Enter new Gamelet name:");
        ImGui.InputText("##newname", ref newGameletName, 64);

        if (ImGui.Button("Create") && !string.IsNullOrEmpty(newGameletName))
        {
            CreateGamelet(newGameletName);
            newGameletName = "";
            selectedIndex = gameletNames.Count - 1;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();

        if (ImGui.Button("Cancel"))
        {
            newGameletName = "";
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

// --- ConfirmDelete ---
    ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
    if (ImGui.BeginPopupModal("ConfirmDelete", ImGuiWindowFlags.AlwaysAutoResize))
    {
        ImGui.Text("Are you sure you want to delete this Gamelet?");

        if (ImGui.Button("Yes"))
        {
            DeleteGamelet();
            if (gameletNames.Count > 0)
                selectedIndex = Math.Min(selectedIndex, gameletNames.Count - 1);
            else
                selectedIndex = -1;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();

        if (ImGui.Button("Cancel"))
            ImGui.CloseCurrentPopup();

        ImGui.EndPopup();
    }


    ImGui.End(); // Main window
    ImGui.PopStyleColor(13);
}




    // === Simulated Methods ===
    private void LoadGamelet(string name)
    {
        // Dummy load
        editingData = new GameletData();
        Console.WriteLine($"Loaded {name}");
    }

    private void SaveGamelet()
    {
        Console.WriteLine($"Saved {gameletNames[selectedIndex]}");
    }

    private void CreateGamelet(string name)
    {
        gameletNames.Add(name);
        selectedIndex = gameletNames.Count - 1;
        Console.WriteLine($"Created {name}");
    }

    private void DeleteGamelet()
    {
        if (gameletNames.Count > 0)
        {
            Console.WriteLine($"Deleted {gameletNames[selectedIndex]}");
            gameletNames.RemoveAt(selectedIndex);
            selectedIndex = Math.Max(0, selectedIndex - 1);
        }
    }

    private struct GameletData
    {
        public int startingPoints, pointGain, pointLoss, maxSpawnCount;
        public float baseRadius, radiusGrowth, spawnCountInterval, maxSpawnInterval, minSpawnInterval, intervalReduction;
    }
}

