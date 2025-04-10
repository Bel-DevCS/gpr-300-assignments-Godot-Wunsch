using Godot;
using Godot.Collections;

public enum EditMode
{
    MeshEditing,
    SkeletonEditing
}

public static class EditSystem
{
    public static EditMode CurrentMode { get; private set; } = EditMode.MeshEditing;

    private static Node _selectedObject;
    private static Dictionary<EditMode, Node> _editors = new();

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

    public static void Undo()
    {
        if (_editors.TryGetValue(CurrentMode, out var editor))
        {
            editor.Call("Undo");
        }
    }

    public static void Redo()
    {
        if (_editors.TryGetValue(CurrentMode, out var editor))
        {
            editor.Call("Redo");
        }
    }

    public static Node GetSelected() => _selectedObject;
    public static Node GetEditor() => _editors.TryGetValue(CurrentMode, out var e) ? e : null;
}