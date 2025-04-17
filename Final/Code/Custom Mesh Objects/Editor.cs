using Godot;
using System;
using System.Collections.Generic;

public partial class Editor : Node
{
    private int _runtimeUndoBaseline = 0;

    public void Undo()
    {
        if (EditSystem.GetUndoCount(EditMode.MeshEditing) <= _runtimeUndoBaseline)
        {
            GD.Print("[Undo] At baseline — cannot undo further.");
            return;
        }

        var obj = EditSystem.PopUndo(EditMode.MeshEditing);
        if (obj is not PointEdit edit) return;

        GD.Print($"[Undo] Undoing {edit.Action} at index {edit.Index}");

        switch (edit.Action)
        {
            case EditAction.Move:
            case EditAction.Rotate:
            case EditAction.Scale:
                // Implement undo for transformations
                break;

            case EditAction.Add:
            case EditAction.Remove:
                // Implement undo for adding/removing points
                break;
        }
    }

    public void Redo()
    {
        var obj = EditSystem.PopRedo(EditMode.MeshEditing);
        if (obj is not PointEdit edit) return;

        GD.Print($"[Redo] Redoing {edit.Action} at index {edit.Index}");

        switch (edit.Action)
        {
            case EditAction.Move:
            case EditAction.Rotate:
            case EditAction.Scale:
                // Implement redo for transformations
                break;

            case EditAction.Add:
            case EditAction.Remove:
                // Implement redo for adding/removing points
                break;
        }
    }

    public struct PointEdit
    {
        public EditAction Action;
        public int Index;
        public PointGenerator.ControlPoint State;
    }

    public enum EditAction
    {
        Move,
        Rotate,
        Scale,
        Add,
        Remove,
        Clear
    }
}