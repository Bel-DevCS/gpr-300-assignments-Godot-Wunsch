using Godot;
using System;
using System.Collections.Generic;

public partial class EdgeNode : Node3D
{
    public PointNode PointA { get; private set; }
    public PointNode PointB { get; private set; }

    public List<FaceNode> ConnectedFaces { get; private set; } = new();

    public LineBuilder Line { get; private set; } = new LineBuilder();
    private MeshInstance3D _lineMesh;
    private StandardMaterial3D _lineMaterial;

    public override void _Ready()
    {
        _lineMaterial = new StandardMaterial3D
        {
            AlbedoColor = Colors.Yellow,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
        };

        _lineMesh = Line.CreateMeshInstance();
        _lineMesh.MaterialOverride = _lineMaterial;
        AddChild(_lineMesh);
    }

    public void SetPoints(PointNode a, PointNode b)
    {
        PointA = a;
        PointB = b;
        UpdateEdge();
    }

    public void UpdateEdge()
    {
        if (PointA == null || PointB == null)
            return;

        Line.SetEndpoints(PointA.GlobalPosition, PointB.GlobalPosition);
        Line.DrawLine();

        if (_lineMesh != null)
        {
            _lineMesh.Mesh = Line.GetImmediateMesh(); // Add getter below
            _lineMesh.MaterialOverride = _lineMaterial;
        }

        foreach (var face in ConnectedFaces)
            face.UpdateFace();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (_lineMaterial != null)
            _lineMaterial.AlbedoColor = highlighted ? Colors.Red : Colors.Yellow;
    }

    public void RegisterFace(FaceNode face)
    {
        if (!ConnectedFaces.Contains(face))
            ConnectedFaces.Add(face);
    }
    
    public void SetEdgeVisible(bool visible)
    {
        if (_lineMesh != null)
            _lineMesh.Visible = visible;
    }

}
