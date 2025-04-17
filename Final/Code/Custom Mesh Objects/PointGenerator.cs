using Godot;
using System;
using System.Collections.Generic;

public partial class PointGenerator : Node
{
    private List<ControlPoint> _points = new();

    // Selection and Reorder state
    private int _selectedPointIndex = -1;
    private int _pendingReorderFrom = -1;
    private int _pendingReorderTo = -1;

    // Adds a new point
    public void AddPoint(Vector3 position)
    {
        var point = new ControlPoint
        {
            Label = $"Point {_points.Count}",
            Position = position,
            Rotation = Basis.Identity,
            Scale = Vector3.One
        };
        _points.Add(point);
    }

    // Removes the point at the specified index
    public void RemovePoint(int index)
    {
        if (index >= 0 && index < _points.Count)
        {
            _points.RemoveAt(index);
        }
    }

    // Updates a point's position
    public void UpdatePoint(int index, Vector3 newPosition)
    {
        if (index >= 0 && index < _points.Count)
        {
            _points[index].Position = newPosition;
        }
    }

    // Get all points
    public List<ControlPoint> GetAllPoints() => _points;

    // Get a specific point
    public ControlPoint GetPoint(int index) => _points[index];

    // Selection state
    public int SelectedPointIndex { get => _selectedPointIndex; set => _selectedPointIndex = value; }

    // Reorder state
    public int PendingReorderFrom { get => _pendingReorderFrom; set => _pendingReorderFrom = value; }
    public int PendingReorderTo { get => _pendingReorderTo; set => _pendingReorderTo = value; }

    // ControlPoint class
    public class ControlPoint
    {
        public string Label { get; set; }
        public Vector3 Position { get; set; }
        public Basis Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public Transform3D ToTransform()
        {
            return new Transform3D(Rotation.Scaled(Scale), Position);
        }
    }
}