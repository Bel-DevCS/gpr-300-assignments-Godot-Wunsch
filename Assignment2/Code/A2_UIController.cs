using Godot;
using System;

public partial class A2_UIController : Node
{
    // Exported node paths for easy assignment in the editor.
    [ExportCategory("Main")]
    [Export]  private Light3D _light;
    
    [ExportCategory("UI")]
    [ExportGroup("UI Panels")] 
    [Export]  private Panel _lightControlPanel;
    [Export]  private Panel _shadowControlPanel;
    [Export]  private Panel _lightColourPanel;
    [Export]  private Panel _shadowPreviewPanel;
    [Export] public NodePath UITogglePath;
  
    
    // Light control UI nodes
    [ExportGroup("Light Control UI")] 
    [Export] private HSlider _xSlider;
    [Export] private Label _xLabel;
    [Export] private HSlider _ySlider;
    [Export] private Label _yLabel;
    [Export] private HSlider _zSlider;
    [Export] private Label _zLabel;
    
    
    [ExportGroup("Light Colour and Intensity UI")]
    [Export] private ColorPicker _lightColourPicker;
    [Export] private HSlider _intensitySlider;
    
    // Shadow control UI nodes.
    [ExportGroup("Shadow Control UI")]
    [Export]  private HSlider _minBiasSlider;
    [Export] private Label _minBiasLabel;
    [Export]  private HSlider _maxBiasSlider;
    [Export] private Label _maxBiasLabel;
    
    //Shadow Map Previewer
    [ExportGroup("Shadow Map Prieview")]
    [Export]    private SubViewportContainer _shadowMapContainer;

    [ExportGroup("UI Toggler")] 
    [Export] public CheckButton LightPos;
    [Export] public CheckButton LightCol;
    [Export] public CheckButton Shadows;
    [Export] public CheckButton ShadowViewer;
    
    private Vector3 _baseRotation;
    
    public override void _Ready()
    {
        
        // Initialize the UI with the current light properties.
        _baseRotation = _light.RotationDegrees;
        
        _minBiasSlider.Value = _light.ShadowBias;
        _maxBiasSlider.Value = _light.ShadowNormalBias;

        _intensitySlider.Value = _light.LightEnergy;
        
        _lightColourPicker.Color = _light.LightColor;
        _lightColourPicker.Connect("color_changed", new Callable(this, nameof(OnLightColorChanged)));
    }
    
    public override void _Process(double delta)
    {
        _lightControlPanel.Visible = LightPos.IsPressed();
        _shadowControlPanel.Visible = Shadows.IsPressed();
        _lightColourPanel.Visible = LightCol.IsPressed();
        _shadowPreviewPanel.Visible = ShadowViewer.IsPressed();
        
        
        // Build a new rotation from the slider values.
        Vector3 offset = new Vector3(
            (float)_xSlider.Value,
            (float)_ySlider.Value,
            (float)_zSlider.Value);
        
        // Update the light's rotation.
        _light.RotationDegrees = _baseRotation + offset;
        
        // Update shadow bias settings.
        _light.ShadowBias = (float)_minBiasSlider.Value;
        _light.ShadowNormalBias = (float)_maxBiasSlider.Value;
        
        _xLabel.Text = "X Value : " + _xSlider.Value.ToString();
        _yLabel.Text = "Y Value : " + _ySlider.Value.ToString();
        _zLabel.Text = "Z Value : " + _zSlider.Value.ToString();
        
        _minBiasLabel.Text = "Min Bias : " + _minBiasSlider.Value.ToString();
        _maxBiasLabel.Text = "Max Bias : " + _maxBiasSlider.Value.ToString();
        
        
        _light.LightEnergy = (float)_intensitySlider.Value;
    }
    
    private void OnLightColorChanged(Color newColor)
    {
        _light.LightColor = newColor;
        GD.Print("Light color updated: " + newColor.ToString());
    }
}
