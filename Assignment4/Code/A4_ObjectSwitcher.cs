using Godot;
using System;
using System.Collections.Generic;

public partial class A4_ObjectSwitcher : Node
{
    private List<MeshInstance3D> models = new();
    private int currentModelIndex = 0;
    private Vector3 globalOffset = Vector3.Zero; // Tracks the position offset
    
    private Dictionary<MeshInstance3D, List<Color>> originalMaterialColors = new(); 
    private Dictionary<MeshInstance3D, List<Color>> modifiedMaterialColors = new();

    // Event to notify UI updates
    public event Action ModelChanged;

    public override void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            if (child is MeshInstance3D meshInstance)
            {
                models.Add(meshInstance);
                EnsureUniqueMaterials(meshInstance); // ✅ Make materials unique
                StoreOriginalMaterialColors(meshInstance);
            }
        }

        SetModel(0);
    }

    /// 🔹 **Ensures each object has a unique instance of its materials**
    private void EnsureUniqueMaterials(MeshInstance3D model)
    {
        int surfaceCount = model.Mesh?.GetSurfaceCount() ?? 0;

        for (int i = 0; i < surfaceCount; i++)
        {
            Material originalMaterial = model.GetSurfaceOverrideMaterial(i) ?? model.Mesh.SurfaceGetMaterial(i);

            if (originalMaterial == null) continue;

            // ✅ Always create a unique material, even if local to scene
            Material newMaterial = originalMaterial.Duplicate() as Material;
            newMaterial.ResourceLocalToScene = true; // ✅ Force it to be unique per object
            model.SetSurfaceOverrideMaterial(i, newMaterial);
        }
    }


    private void StoreOriginalMaterialColors(MeshInstance3D model)
    {
        List<Color> colors = new();
        int surfaceCount = model.Mesh?.GetSurfaceCount() ?? 0;

        for (int i = 0; i < surfaceCount; i++)
        {
            Material material = model.GetSurfaceOverrideMaterial(i) ?? model.Mesh.SurfaceGetMaterial(i);
            if (material is StandardMaterial3D standardMaterial)
            {
                colors.Add(standardMaterial.AlbedoColor);
            }
            else
            {
                colors.Add(Colors.White);
            }
        }

        originalMaterialColors[model] = colors;
    }

    public async void SetModel(int index)
    {
        if (index < 0 || index >= models.Count) return;
        if (index == currentModelIndex) return;

        MeshInstance3D currentModel = models[currentModelIndex];

        await ToSignal(GetTree(), "process_frame");

        currentModel.Visible = false;
        await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

        foreach (var model in models) model.Visible = false;
        models[index].Visible = true;
        currentModelIndex = index;

        EnsureUniqueMaterials(models[index]); // ✅ Ensure materials are unique on switch
        ApplyMaterialColors(models[index]);

        ModelChanged?.Invoke();
    }

    private void ApplyMaterialColors(MeshInstance3D model)
    {
        if (!modifiedMaterialColors.ContainsKey(model)) return;

        List<Color> colors = modifiedMaterialColors[model];
        int surfaceCount = model.Mesh?.GetSurfaceCount() ?? 0;

        for (int i = 0; i < surfaceCount; i++)
        {
            Material material = model.GetSurfaceOverrideMaterial(i);
            if (material == null)
            {
                material = model.Mesh.SurfaceGetMaterial(i)?.Duplicate() as Material;
                model.SetSurfaceOverrideMaterial(i, material);
            }

            if (material is StandardMaterial3D standardMaterial && i < colors.Count)
            {
                standardMaterial.AlbedoColor = colors[i];
            }
        }
    }

    public void ChangeMaterialColor(int materialIndex, Color newColor)
    {
        if (models.Count == 0) return;
        MeshInstance3D selectedModel = models[currentModelIndex];

        if (selectedModel.Mesh == null)
        {
            GD.PrintErr("Mesh not found on selected model!");
            return;
        }

        int surfaceCount = selectedModel.Mesh.GetSurfaceCount();
        if (materialIndex < 0 || materialIndex >= surfaceCount)
        {
            GD.PrintErr($"Invalid material index {materialIndex}. Model has {surfaceCount} surfaces.");
            return;
        }

        Material material = selectedModel.GetSurfaceOverrideMaterial(materialIndex);
        if (material == null)
        {
            material = selectedModel.Mesh.SurfaceGetMaterial(materialIndex)?.Duplicate() as Material;
            selectedModel.SetSurfaceOverrideMaterial(materialIndex, material);
        }

        if (material is StandardMaterial3D standardMaterial)
        {
            standardMaterial.AlbedoColor = newColor;

            if (!modifiedMaterialColors.ContainsKey(selectedModel))
                modifiedMaterialColors[selectedModel] = new List<Color>();

            while (modifiedMaterialColors[selectedModel].Count <= materialIndex)
                modifiedMaterialColors[selectedModel].Add(Colors.White);

            modifiedMaterialColors[selectedModel][materialIndex] = newColor;
        }
        else
        {
            GD.PrintErr("Material is not a StandardMaterial3D!");
        }
    }

    public void NextModel()
    {
        int nextIndex = (currentModelIndex + 1) % models.Count;
        SetModel(nextIndex);
    }

    public void LastModel()
    {
        int nextIndex = (currentModelIndex - 1 + models.Count) % models.Count;
        SetModel(nextIndex);
    }

    public MeshInstance3D GetCurrentModel()
    {
        return models.Count > 0 ? models[currentModelIndex] : null;
    }

    public List<Material> GetAllMaterials()
    {
        List<Material> materials = new();
        if (models.Count == 0) return materials;
        MeshInstance3D selectedModel = models[currentModelIndex];

        if (selectedModel.Mesh == null)
        {
            GD.PrintErr("Mesh not found on selected model!");
            return materials;
        }

        int surfaceCount = selectedModel.Mesh.GetSurfaceCount();
        for (int i = 0; i < surfaceCount; i++)
        {
            Material material = selectedModel.GetSurfaceOverrideMaterial(i);
            if (material == null)
            {
                material = selectedModel.Mesh.SurfaceGetMaterial(i);
            }
            if (material != null)
            {
                materials.Add(material);
            }
        }
        return materials;
    }

    public void MoveModels(Vector3 newPosition)
    {
        globalOffset = newPosition;
        foreach (var model in models)
        {
            model.GlobalPosition = newPosition;
        }
    }

    public Vector3 GetPosition()
    {
        return models.Count > 0 ? models[0].GlobalPosition : Vector3.Zero;
    }

    public void SetRotation(Vector3 newRotation)
    {
        foreach (var model in models)
        {
            model.RotationDegrees = newRotation;
        }
    }

    public Vector3 GetRotation()
    {
        return models.Count > 0 ? models[0].RotationDegrees : Vector3.Zero;
    }

    public void SetScale(Vector3 newScale)
    {
        foreach (var model in models)
        {
            model.Scale = newScale;
        }
    }

    public Vector3 GetScale()
    {
        return models.Count > 0 ? models[0].Scale : Vector3.One;
    }

    public List<string> GetMeshNames()
    {
        List<string> names = new();
        foreach (var model in models)
        {
            names.Add(model.Name);
        }
        return names;
    }

    public MeshInstance3D GetMeshInstanceByName(string meshName)
    {
        foreach (var model in models)
        {
            if (model.Name == meshName)
            {
                return model;
            }
        }
        return null;
    }
}
