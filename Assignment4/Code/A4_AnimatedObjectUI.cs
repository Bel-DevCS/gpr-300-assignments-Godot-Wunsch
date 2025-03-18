using Godot;
using System;
using System.Collections.Generic;

public partial class A4_AnimatedObjectUI : CanvasLayer
{
    [Export] private Panel MaterialPanel; // UI Panel
    [Export] private Node ObjectNode; // Object Switcher
    
    [Export] private A4_AnimatedObject animatedObject;

    private VBoxContainer rowContainer; // Holds multiple material lists
    private ScrollContainer scrollContainer;
    
    [Export] private Panel PRSPanel; // Panel that holds Position, Rotation, and Scale Information
    private VBoxContainer positionControls; 
    
    [Export] private Panel MeshSelectorPanel;

    [Export] private Panel KeyframePanel; // Assign in Godot UI
    private Label timeLabel; // Displays animation time
    private VBoxContainer keyframeListContainer; 
    

    public override void _Ready()
    {
       InitMaterialInfoPanel();
       InitPRSPanel();
       InitMeshSelectorPanel();
       InitKeyframeUIPanel();
    }

    #region Material
      void InitMaterialInfoPanel()
    {
        if (MaterialPanel == null)
        {
            GD.PrintErr("MaterialPanel is not assigned!");
            return;
        }

        // 🔹 Create a ScrollContainer (for scrolling if needed)
        ScrollContainer scrollContainer = new ScrollContainer();
        scrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scrollContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        MaterialPanel.AddChild(scrollContainer);

        // 🔹 Create a VBoxContainer inside ScrollContainer
        VBoxContainer materialContainer = new VBoxContainer();
        materialContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        materialContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        materialContainer.AddThemeConstantOverride("separation", 10); // Adds spacing
        scrollContainer.AddChild(materialContainer);

        // 🔹 Add a Label at the top
        Label panelTitle = new Label
        {
            Text = "Material Properties",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        materialContainer.AddChild(panelTitle);

        // 🔹 Set `rowContainer` to hold dynamic material rows
        rowContainer = new VBoxContainer();
        rowContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        materialContainer.AddChild(rowContainer);

        // 🔹 Ensure UI updates when model changes
        if (ObjectNode is A4_ObjectSwitcher objSwitcher)
        {
            objSwitcher.ModelChanged += ModifyMaterialInfo;
        }

        ModifyMaterialInfo(); // Initial UI setup
    }
   void ModifyMaterialInfo()
{
    // 🔹 Clear old UI elements
    foreach (Node child in rowContainer.GetChildren())
    {
        child.QueueFree();
    }

    if (ObjectNode is A4_ObjectSwitcher objSwitcher)
    {
        MeshInstance3D currentModel = objSwitcher.GetCurrentModel();
        if (currentModel == null) return;

        var materials = objSwitcher.GetAllMaterials();
        
        // Create a VBoxContainer for each material set
        VBoxContainer materialList = new VBoxContainer();
        rowContainer.AddChild(materialList);

        // Add a Label for the Object Name
        Label objectLabel = new Label { Text = $"Editing: {currentModel.Name}" };
        materialList.AddChild(objectLabel);

        //  Create UI for Each Material
        for (int i = 0; i < materials.Count; i++)
        {
            Material material = materials[i];

            // Store original material color
            Color defaultColor = material is StandardMaterial3D standardMaterial ? standardMaterial.AlbedoColor : Colors.White;

            // Create an HBox for each material row
            HBoxContainer materialRow = new HBoxContainer();
            materialRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

            //  Material Name Label
            Label matLabel = new Label { Text = $"Material {i}" };
            matLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            materialRow.AddChild(matLabel);

            //  Reset Button
            Button resetButton = new Button();
            resetButton.Text = "✖️";
            resetButton.TooltipText = "Reset Material Colour";
            resetButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;

            // Colour Preview Box
            ColorRect colorPreview = new ColorRect();
            colorPreview.CustomMinimumSize = new Vector2(30, 30); // Set a square box size
            colorPreview.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            colorPreview.Color = defaultColor;

            // Color Picker Button
            ColorPickerButton colorPicker = new ColorPickerButton();
            colorPicker.Color = defaultColor;

            int materialIndex = i; // Fix lambda scope issue

            // Event: Update Material Color
            colorPicker.ColorChanged += (Color newColor) =>
            {
                objSwitcher.ChangeMaterialColor(materialIndex, newColor);
                colorPreview.Color = newColor; // Update the preview box color
            };

            // Event: Reset Material Color
            resetButton.Pressed += () =>
            {
                objSwitcher.ChangeMaterialColor(materialIndex, defaultColor);
                colorPreview.Color = defaultColor;
                colorPicker.Color = defaultColor;
            };

            // Add UI Elements in Order
            materialRow.AddChild(colorPicker);   // Colour picker
            materialRow.AddChild(colorPreview);  // Colour preview box
            materialRow.AddChild(resetButton);   // Reset button
            materialList.AddChild(materialRow);  // Add row to material list
        }
    }
}
    #endregion
    #region Position, Rotation, and Scale
     void InitPRSPanel()
{
    if (PRSPanel == null)
    {
        GD.PrintErr("PRSPanel is not assigned!");
        return;
    }

    // 🔹 Create a ScrollContainer to allow scrolling
    ScrollContainer scrollContainer = new ScrollContainer();
    scrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
    scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    scrollContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect); // Fill entire panel
    PRSPanel.AddChild(scrollContainer);

    // 🔹 Create a VBoxContainer for organized layout inside ScrollContainer
    VBoxContainer prsContainer = new VBoxContainer();
    prsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
    prsContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    prsContainer.AddThemeConstantOverride("separation", 10); // Adds spacing

    scrollContainer.AddChild(prsContainer); // Add VBoxContainer inside ScrollContainer

    // 🔹 Add a Label at the top
    Label panelTitle = new Label
    {
        Text = "Transform Controls",
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };
    prsContainer.AddChild(panelTitle);

    // 🔹 Create separate containers for Position, Rotation, and Scale
    positionControls = new VBoxContainer();
    positionControls.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
    positionControls.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    prsContainer.AddChild(positionControls);

    VBoxContainer rotationControls = new VBoxContainer();
    rotationControls.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
    rotationControls.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    prsContainer.AddChild(rotationControls);

    VBoxContainer scaleControls = new VBoxContainer();
    scaleControls.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
    scaleControls.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    prsContainer.AddChild(scaleControls);

    // Generate UI for Position, Rotation, and Scale
    CreatePositionControl("X", 0);
    CreatePositionControl("Y", 1);
    CreatePositionControl("Z", 2);

    CreateRotationControl(rotationControls, "X", 0);
    CreateRotationControl(rotationControls, "Y", 1);
    CreateRotationControl(rotationControls, "Z", 2);

    CreateScaleControl(scaleControls, "X", 0);
    CreateScaleControl(scaleControls, "Y", 1);
    CreateScaleControl(scaleControls, "Z", 2);

    // Add Reset Buttons
    Button resetPositionButton = new Button { Text = "Reset Position", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    resetPositionButton.Pressed += ResetPosition;
    prsContainer.AddChild(resetPositionButton);

    Button resetRotationButton = new Button { Text = "Reset Rotation", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    resetRotationButton.Pressed += ResetRotation;
    prsContainer.AddChild(resetRotationButton);

    Button resetScaleButton = new Button { Text = "Reset Scale", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    resetScaleButton.Pressed += ResetScale;
    prsContainer.AddChild(resetScaleButton);
}
    private void CreatePositionControl(string axis, int axisIndex)
    {
        HBoxContainer controlRow = new HBoxContainer();
        controlRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        controlRow.AddThemeConstantOverride("separation", 10); // Adds spacing between elements

        Label axisLabel = new Label
        {
            Text = $"Position {axis}:",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        controlRow.AddChild(axisLabel);

        // Slider for Position
        HSlider positionSlider = new HSlider
        {
            MinValue = -100.0,  // Min position
            MaxValue = 100.0,   // Max position
            Step = 0.1,         // Precision
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        Label valueLabel = new Label
        {
            Text = "0.0",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd
        };

        // Set initial value
        if (ObjectNode is A4_ObjectSwitcher objSwitcher)
        {
            Vector3 currentPosition = objSwitcher.GetPosition();
            positionSlider.Value = axisIndex == 0 ? currentPosition.X :
                axisIndex == 1 ? currentPosition.Y :
                currentPosition.Z;
            valueLabel.Text = positionSlider.Value.ToString("F1");
        }

        // 🔹 Update position dynamically
        positionSlider.ValueChanged += (double newValue) =>
        {
            if (ObjectNode is A4_ObjectSwitcher objSwitcher)
            {
                Vector3 newPos = objSwitcher.GetPosition();
                if (axisIndex == 0) newPos.X = (float)newValue;
                else if (axisIndex == 1) newPos.Y = (float)newValue;
                else newPos.Z = (float)newValue;

                objSwitcher.MoveModels(newPos);
                valueLabel.Text = newValue.ToString("F1"); // Update label
            }
        };

        controlRow.AddChild(positionSlider);
        controlRow.AddChild(valueLabel);
        positionControls.AddChild(controlRow);
    }
    private void CreateRotationControl(VBoxContainer container, string axis, int axisIndex)
    {
        HBoxContainer controlRow = new HBoxContainer();
        controlRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        controlRow.AddThemeConstantOverride("separation", 10);

        Label axisLabel = new Label
        {
            Text = $"Rotation {axis}:",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        controlRow.AddChild(axisLabel);

        // Slider for Rotation
        HSlider rotationSlider = new HSlider
        {
            MinValue = -180.0,  // Min rotation
            MaxValue = 180.0,   // Max rotation
            Step = 1.0,         // Precision
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        Label valueLabel = new Label
        {
            Text = "0.0",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd
        };

        if (ObjectNode is A4_ObjectSwitcher objSwitcher)
        {
            Vector3 currentRotation = objSwitcher.GetRotation();
            rotationSlider.Value = axisIndex == 0 ? currentRotation.X :
                axisIndex == 1 ? currentRotation.Y :
                currentRotation.Z;
            valueLabel.Text = rotationSlider.Value.ToString("F1");
        }

        // Update rotation dynamically
        rotationSlider.ValueChanged += (double newValue) =>
        {
            if (ObjectNode is A4_ObjectSwitcher objSwitcher)
            {
                Vector3 newRot = objSwitcher.GetRotation();
                if (axisIndex == 0) newRot.X = (float)newValue;
                else if (axisIndex == 1) newRot.Y = (float)newValue;
                else newRot.Z = (float)newValue;

                objSwitcher.SetRotation(newRot);
                valueLabel.Text = newValue.ToString("F1"); // Update label
            }
        };

        controlRow.AddChild(rotationSlider);
        controlRow.AddChild(valueLabel);
        container.AddChild(controlRow);
    }
    private void CreateScaleControl(VBoxContainer container, string axis, int axisIndex)
    {
        HBoxContainer controlRow = new HBoxContainer();
        controlRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        controlRow.AddThemeConstantOverride("separation", 10);

        Label axisLabel = new Label
        {
            Text = $"Scale {axis}:",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        controlRow.AddChild(axisLabel);

        // Slider for Scale
        HSlider scaleSlider = new HSlider
        {
            MinValue = 0.1,  // Min scale
            MaxValue = 5.0,  // Max scale
            Step = 0.1,      // Precision
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        Label valueLabel = new Label
        {
            Text = "1.0",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd
        };

        if (ObjectNode is A4_ObjectSwitcher objSwitcher)
        {
            Vector3 currentScale = objSwitcher.GetScale();
            scaleSlider.Value = axisIndex == 0 ? currentScale.X :
                axisIndex == 1 ? currentScale.Y :
                currentScale.Z;
            valueLabel.Text = scaleSlider.Value.ToString("F1");
        }

        // Update scale dynamically
        scaleSlider.ValueChanged += (double newValue) =>
        {
            if (ObjectNode is A4_ObjectSwitcher objSwitcher)
            {
                Vector3 newScale = objSwitcher.GetScale();
                if (axisIndex == 0) newScale.X = (float)newValue;
                else if (axisIndex == 1) newScale.Y = (float)newValue;
                else newScale.Z = (float)newValue;

                objSwitcher.SetScale(newScale);
                valueLabel.Text = newValue.ToString("F1"); // Update label
            }
        };

        controlRow.AddChild(scaleSlider);
        controlRow.AddChild(valueLabel);
        container.AddChild(controlRow);
    }
    void ResetPosition()
    {
        if (ObjectNode is A4_ObjectSwitcher objSwitcher)
        {
            Vector3 resetPos = Vector3.Zero;
            objSwitcher.MoveModels(resetPos);

            // Update UI spinboxes to reflect reset
            foreach (Node child in positionControls.GetChildren())
            {
                if (child is HBoxContainer hbox)
                {
                    foreach (Node element in hbox.GetChildren())
                    {
                        if (element is SpinBox spinBox)
                        {
                            spinBox.Value = 0.0; // Reset to 0
                        }
                    }
                }
            }
        }
    }
    void ResetRotation()
    {
        if (ObjectNode is A4_ObjectSwitcher objSwitcher)
        {
            objSwitcher.SetRotation(Vector3.Zero);

            foreach (Node child in positionControls.GetChildren())
            {
                if (child is HBoxContainer hbox)
                {
                    foreach (Node element in hbox.GetChildren())
                    {
                        if (element is SpinBox spinBox)
                        {
                            spinBox.Value = 0.0;
                        }
                    }
                }
            }
        }
    }
    void ResetScale()
    {
        if (ObjectNode is A4_ObjectSwitcher objSwitcher)
        {
            objSwitcher.SetScale(Vector3.One);

            foreach (Node child in positionControls.GetChildren())
            {
                if (child is HBoxContainer hbox)
                {
                    foreach (Node element in hbox.GetChildren())
                    {
                        if (element is SpinBox spinBox)
                        {
                            spinBox.Value = 1.0;
                        }
                    }
                }
            }
        }
    }
    #endregion
    
    void InitMeshSelectorPanel()
    {
        if (MeshSelectorPanel == null)
        {
            GD.PrintErr("MeshSelectorPanel is not assigned!");
            return;
        }

        // 🔹 Create a VBoxContainer to organize the dropdown and button
        VBoxContainer selectorContainer = new VBoxContainer();
        selectorContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        selectorContainer.AddThemeConstantOverride("separation", 10);

        // 🔹 Create a Label for clarity
        Label selectorLabel = new Label
        {
            Text = "Select Mesh:",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        selectorContainer.AddChild(selectorLabel);

        // 🔹 Create Dropdown (OptionButton)
        OptionButton meshDropdown = new OptionButton
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        // Populate dropdown with available meshes
        if (ObjectNode is A4_ObjectSwitcher objSwitcher)
        {
            List<string> meshNames = objSwitcher.GetMeshNames();
            for (int i = 0; i < meshNames.Count; i++)
            {
                meshDropdown.AddItem(meshNames[i], i);
            }
        }

        selectorContainer.AddChild(meshDropdown);

        // 🔹 Create "Apply Mesh" Button
        Button selectMeshButton = new Button
        {
            Text = "Apply Mesh",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        // 🔹 Connect button press to apply selected mesh
        selectMeshButton.Pressed += () =>
        {
            if (ObjectNode is A4_ObjectSwitcher objSwitcher)
            {
                int selectedIndex = meshDropdown.GetSelectedId();
                objSwitcher.SetModel(selectedIndex);
            }
        };

        selectorContainer.AddChild(selectMeshButton);

        // 🔹 Add UI elements to MeshSelectorPanel
        MeshSelectorPanel.AddChild(selectorContainer);
    }
    
    private void InitEasingDropdown()
    {
        if (KeyframePanel == null)
        {
            GD.PrintErr("KeyframePanel is not assigned!");
            return;
        }

        VBoxContainer easingContainer = new VBoxContainer();
        easingContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        easingContainer.AddThemeConstantOverride("separation", 10);

        Label easingLabel = new Label
        {
            Text = "Easing Mode:",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        easingContainer.AddChild(easingLabel);

        // 🔹 Create Dropdown (OptionButton)
        OptionButton easingDropdown = new OptionButton
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        // Populate Dropdown
        easingDropdown.AddItem("Linear", 0);
        easingDropdown.AddItem("Ease In", 1);
        easingDropdown.AddItem("Ease Out", 2);
        easingDropdown.AddItem("Smooth Step", 3);

        // Ensure dropdown reflects current easing mode
        easingDropdown.Selected = animatedObject.GetEasingType();

        easingDropdown.ItemSelected += (long index) =>
        {
            animatedObject.SetEasingType((int)index);
            GD.Print($"Easing Type Changed: {index}");
        };

        easingContainer.AddChild(easingDropdown);
        KeyframePanel.AddChild(easingContainer);
    }

void InitKeyframeUIPanel()
{
    if (KeyframePanel == null)
    {
        GD.PrintErr("KeyframePanel is not assigned!");
        return;
    }

    VBoxContainer keyframeContainer = new VBoxContainer
    {
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
    };
    keyframeContainer.AddThemeConstantOverride("separation", 10);

    Label panelTitle = new Label
    {
        Text = "Keyframe Controls",
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        HorizontalAlignment = HorizontalAlignment.Center
    };
    keyframeContainer.AddChild(panelTitle);

    // 🔹 Keyframe Buttons
    HBoxContainer keyframeButtonRow = new HBoxContainer();
    keyframeButtonRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

    Button addKeyframeButton = new Button { Text = "➕ Add Keyframe", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    addKeyframeButton.Pressed += () =>
    {
        animatedObject.AddKeyframe();
        UpdateKeyframeList();
    };
    keyframeButtonRow.AddChild(addKeyframeButton);

    keyframeContainer.AddChild(keyframeButtonRow);

    // 🔹 Time Controls (Slider + SpinBox)
    Label timeLabel = new Label
    {
        Text = "Animation Time (s):",
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
    };
    keyframeContainer.AddChild(timeLabel);

    HBoxContainer timeControlRow = new HBoxContainer();
    timeControlRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

    HSlider timeSlider = new HSlider
    {
        MinValue = 0,
        MaxValue = 10, 
        Step = 0.01,
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
    };

    SpinBox timeSpinBox = new SpinBox
    {
        MinValue = 0,
        MaxValue = 10,
        Step = 0.01,
        SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd
    };

    timeSlider.ValueChanged += (double newValue) =>
    {
        timeSpinBox.Value = newValue;
        animatedObject.SetTime((float)newValue);
        animatedObject.SetToCurrentKeyframe();
        UpdateKeyframeInfo();
    };

    timeSpinBox.ValueChanged += (double newValue) =>
    {
        timeSlider.Value = newValue;
        animatedObject.SetTime((float)newValue);
        animatedObject.SetToCurrentKeyframe();
        UpdateKeyframeInfo();
    };

    timeControlRow.AddChild(timeSlider);
    timeControlRow.AddChild(timeSpinBox);
    keyframeContainer.AddChild(timeControlRow);

    // 🔹 Easing Mode Dropdown
    Label easingLabel = new Label
    {
        Text = "Easing Mode:",
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
    };
    keyframeContainer.AddChild(easingLabel);

    OptionButton easingDropdown = new OptionButton
    {
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
    };

    easingDropdown.AddItem("Linear", 0);
    easingDropdown.AddItem("Ease In", 1);
    easingDropdown.AddItem("Ease Out", 2);
    easingDropdown.AddItem("Smooth Step", 3);

    easingDropdown.Selected = animatedObject.GetEasingType();
    easingDropdown.ItemSelected += (long index) =>
    {
        animatedObject.SetEasingType((int)index);
        GD.Print($"Easing Type Changed: {index}");
    };

    keyframeContainer.AddChild(easingDropdown);

    // 🔹 Keyframe List
    keyframeListContainer = new VBoxContainer();
    keyframeListContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
    keyframeContainer.AddChild(keyframeListContainer);

    // 🔹 Keyframe Info
    Label keyframeInfoLabel = new Label
    {
        Text = "Selected Keyframe Data",
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        HorizontalAlignment = HorizontalAlignment.Center
    };
    keyframeContainer.AddChild(keyframeInfoLabel);

    keyframeData = new Label
    {
        Text = "No keyframe selected",
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
    };
    keyframeContainer.AddChild(keyframeData);

    Button applyKeyframeButton = new Button { Text = "Apply Keyframe Data", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    applyKeyframeButton.Pressed += () =>
    {
        animatedObject.SetToCurrentKeyframe();
    };
    keyframeContainer.AddChild(applyKeyframeButton);

    KeyframePanel.AddChild(keyframeContainer);

    // Update UI initially
    UpdateKeyframeList();
}

private void OnEasingTypeChanged(long index)
{
   
    animatedObject.SetEasingType((int)index);
}

private Label keyframeData;

void UpdateKeyframeInfo()
{
    
        KeyframeData? currentKeyframe = animatedObject.GetCurrentKeyframe();

        if (currentKeyframe != null)
        {
            keyframeData.Text = $"Time: {currentKeyframe.Time:F2}s\n" +
                                $"Pos: {currentKeyframe.Position}\n" +
                                $"Rot: {currentKeyframe.Rotation}\n" +
                                $"Scale: {currentKeyframe.Scale}\n" +
                                $"Color: {currentKeyframe.MaterialColor}\n" +
                                $"Mesh: {currentKeyframe.MeshName}";
        }
        else
        {
            keyframeData.Text = "No keyframe selected";
        }
}

void UpdateKeyframeList()
{
    // 🔹 Clear existing UI elements
    foreach (Node child in keyframeListContainer.GetChildren())
    {
        child.QueueFree();
    }

    List<KeyframeData> keyframes = animatedObject.GetKeyframes();

    for (int i = 0; i < keyframes.Count; i++)
    {
        KeyframeData keyframe = keyframes[i];

        HBoxContainer keyframeRow = new HBoxContainer();
        keyframeRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        int index = i; // ✅ Store `i` inside loop to avoid lambda closure issues

        Button selectButton = new Button
        {
            Text = $"⏩ {keyframe.Time:F2}s",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };

        selectButton.Pressed += () =>
        {
            animatedObject.SetTime(keyframes[index].Time);
            UpdateKeyframeInfo();
        };

        Button deleteButton = new Button
        {
            Text = "❌",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd
        };

        deleteButton.Pressed += () =>
        {
            animatedObject.RemoveKeyframeAt(index);
            UpdateKeyframeList();
            UpdateKeyframeInfo();
        };

        keyframeRow.AddChild(selectButton);
        keyframeRow.AddChild(deleteButton);
        keyframeListContainer.AddChild(keyframeRow);
    }
}

private bool isUpdatingTime = false;
private HSlider timeSlider;
private SpinBox timeSpinBox;

void UpdateTimeControls()
{
    if (animatedObject == null) return;

    float currentTime = animatedObject.GetCurrentTime();

    if (!isUpdatingTime)
    {
        isUpdatingTime = true;
        timeSlider.Value = currentTime;
        timeSpinBox.Value = currentTime;
        animatedObject.SetTime(currentTime);  // 🔹 Ensure object updates immediately
        UpdateKeyframeInfo();
        isUpdatingTime = false;
    }
}

void UpdatePlayPauseUI(bool isPlaying)
{
    foreach (Node child in KeyframePanel.GetChildren())
    {
        if (child is HBoxContainer buttonRow)
        {
            foreach (Node button in buttonRow.GetChildren())
            {
                if (button is Button btn)
                {
                    if (btn.Text == "▶ Play")
                        btn.Disabled = isPlaying;  // Disable Play when playing
                    if (btn.Text == "⏸ Pause")
                        btn.Disabled = !isPlaying; // Disable Pause when paused
                }
            }
        }
    }
}

void initFunToggles()
{
    
}

}
