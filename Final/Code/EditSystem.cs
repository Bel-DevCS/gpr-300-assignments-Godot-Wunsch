using System.Collections.Generic;
using Godot;

public enum EditMode
{
    MeshEditing,
    SkeletonEditing
}

public static class EditSystem
{
    public static EditMode CurrentMode { get; private set; } = EditMode.MeshEditing;

    private static Node _selectedObject;
    private static Godot.Collections.Dictionary<EditMode, Node> _editors = new();

    public static void SetMode(EditMode mode)
    {
        CurrentMode = mode;
        GD.Print($"[EditSystem] Mode changed to {mode}");
    }

    public static void RegisterEditor(EditMode mode, Node editor)
    {
        _editors[mode] = editor;
    }

    public static void Select(Node node)
    {
        _selectedObject = node;
        GD.Print($"[EditSystem] Selected {node.Name}");
    }

    public static void Deselect()
    {
        _selectedObject = null;
    }

    private static Dictionary<EditMode, Stack<object>> _undoStacks = new();
    private static Dictionary<EditMode, Stack<object>> _redoStacks = new();

    private const int MAX_HISTORY = 100;

    public static void PushUndo(EditMode mode, object edit)
    {
        if (!_undoStacks.ContainsKey(mode))
            _undoStacks[mode] = new Stack<object>();

        _undoStacks[mode].Push(edit);
        if (_undoStacks[mode].Count > MAX_HISTORY)
            _undoStacks[mode] = new Stack<object>(_undoStacks[mode].ToArray()[..MAX_HISTORY]);

        // Clear redo on new change
        if (_redoStacks.ContainsKey(mode))
            _redoStacks[mode].Clear();
    }

    public static object PopUndo(EditMode mode)
    {
        return _undoStacks.ContainsKey(mode) && _undoStacks[mode].Count > 0
            ? _undoStacks[mode].Pop()
            : null;
    }

    public static void PushRedo(EditMode mode, object edit)
    {
        if (!_redoStacks.ContainsKey(mode))
            _redoStacks[mode] = new Stack<object>();

        _redoStacks[mode].Push(edit);
    }

    public static object PopRedo(EditMode mode)
    {
        return _redoStacks.ContainsKey(mode) && _redoStacks[mode].Count > 0
            ? _redoStacks[mode].Pop()
            : null;
    }

    public static int GetUndoCount(EditMode mode)
    {
        return _undoStacks.ContainsKey(mode) ? _undoStacks[mode].Count : 0;
    }


    public static Node GetSelected() => _selectedObject;
    public static Node GetEditor() => _editors.TryGetValue(CurrentMode, out var e) ? e : null;
}