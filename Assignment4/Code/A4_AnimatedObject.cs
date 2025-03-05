using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class A4_AnimatedObject : Node3D
{
    [Export] private A4_ObjectSwitcher objectSwitcher; // Reference to object switcher
    
    [Export] private CanvasLayer ui;  // Expose the UI in the Inspector

    public CanvasLayer GetUI() => ui;  // Allow `MainScene` to toggle UI visibility
    

    private List<KeyframeData> keyframes = new();
    private float currentTime = 0f;
    private bool isPlaying = false;
    private int easingType = 2;  // Default: Ease Out
    
    [Export] private A4_SwapParticle particleEffect;

    public override void _Ready()
    {
        SetUIVisibility(false);
    }
    public override void _Process(double delta)
    {
        if (!isPlaying) return; // ✅ Stop updates when paused

        currentTime += (float)delta;

        if (keyframes.Count > 0 && currentTime > keyframes[keyframes.Count - 1].Time)
        {
            isPlaying = false;  // Stop if we pass the last keyframe
            currentTime = keyframes[keyframes.Count - 1].Time;
        }

        ApplyInterpolatedFrame(currentTime);
    }

    public void SetUIVisibility(bool visible)
    {
        if (ui != null)
        {
            ui.Visible = visible;
        }
    }

    public bool IsPlaying() => isPlaying;


    public void AddKeyframe()
    {
        if (objectSwitcher == null) return;

        float newKeyframeTime = currentTime;

        string currentMeshName = objectSwitcher.GetCurrentModel()?.Name ?? "Unknown";

        KeyframeData newKeyframe = new KeyframeData(
            newKeyframeTime,
            objectSwitcher.GetPosition(),
            objectSwitcher.GetRotation(),
            objectSwitcher.GetScale(),
            objectSwitcher.GetAllMaterials().Count > 0 && objectSwitcher.GetAllMaterials()[0] is StandardMaterial3D mat
                ? mat.AlbedoColor
                : Colors.White,
            currentMeshName);

        keyframes.Add(newKeyframe);
        keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));

        GD.Print($"Keyframe Added at {newKeyframeTime}s | Total Keyframes: {keyframes.Count}");
    }
    


    public void RemoveLastKeyframe()
    {
        if (keyframes.Count > 0)
        {
            keyframes.RemoveAt(keyframes.Count - 1);
            GD.Print("Last Keyframe Removed.");
        }
    }

    private int originalModelIndex = -1; // Store initial model

    public void PlayAnimation()
    {
        if (keyframes.Count < 2)
        {
            GD.PrintErr("Not enough keyframes to play animation!");
            return;
        }

        if (originalModelIndex == -1) // Store original model only once
            originalModelIndex = objectSwitcher.GetMeshNames().IndexOf(objectSwitcher.GetCurrentModel().Name);
        
        currentTime = keyframes[0].Time;
        ApplyInterpolatedFrame(currentTime);
        isPlaying = true;
    }

    public void PauseAnimation()
    {
        isPlaying = false;

        // Restore original model when animation stops
        if (originalModelIndex != -1)
        {
            objectSwitcher.SetModel(originalModelIndex);
            originalModelIndex = -1; // Reset
        }
    }


    public void SetTime(float newTime)
    {
        if (keyframes.Count == 0) return;  

        currentTime = newTime; // 🔹 Ensure manual edits in UI update `currentTime`

        if (keyframes.Count == 1)
        {
            KeyframeData singleKeyframe = keyframes[0];
            objectSwitcher.MoveModels(singleKeyframe.Position);
            objectSwitcher.SetRotation(singleKeyframe.Rotation);
            objectSwitcher.SetScale(singleKeyframe.Scale);
            objectSwitcher.ChangeMaterialColor(0, singleKeyframe.MaterialColor);
            objectSwitcher.SetModel(objectSwitcher.GetMeshNames().IndexOf(singleKeyframe.MeshName));
        }
        else
        {
            ApplyInterpolatedFrame(currentTime);
        }
    }




    public void SetEasingType(int type)
    {
        easingType = type;
    }

    private void ApplyInterpolatedFrame(float time)
    {
        if (keyframes.Count == 0) return;

        KeyframeData previous = null;
        KeyframeData next = null;

        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            if (time >= keyframes[i].Time && time < keyframes[i + 1].Time)
            {
                previous = keyframes[i];
                next = keyframes[i + 1];
                break;
            }
        }

        if (previous == null || next == null)
        {
            isPlaying = false;
            return;
        }

        float t = (time - previous.Time) / (next.Time - previous.Time);
        t = ApplyEasingFunction(t);

        objectSwitcher.MoveModels(previous.Position.Lerp(next.Position, t));
        objectSwitcher.SetRotation(previous.Rotation.Lerp(next.Rotation, t));
        objectSwitcher.SetScale(previous.Scale.Lerp(next.Scale, t));
        objectSwitcher.ChangeMaterialColor(0, previous.MaterialColor.Lerp(next.MaterialColor, t));

        //  Check if a model change is needed
        if (previous.MeshName != next.MeshName)
        {
            if (t >= 0.4f)
            {
                if (particleEffect != null)
                {
                    particleEffect.TriggerEffect(objectSwitcher.GetCurrentModel().GlobalTransform.Origin);
                }
            }

            if (t >= 0.5f)
            {
                objectSwitcher.SetModel(objectSwitcher.GetMeshNames().IndexOf(next.MeshName));
            }
        }

    }



    private float ApplyEasingFunction(float t)
    {
        return easingType switch
        {
            0 => t, // Linear
            1 => t * t, // Ease In
            2 => 1 - Mathf.Cos((t * Mathf.Pi) / 2), // Ease Out
            3 => Mathf.SmoothStep(0, 1, t), // SmoothStep
            _ => t,
        };
    }
    
    public void SetToCurrentKeyframe()
    {
        if (objectSwitcher == null || keyframes.Count == 0) return;

        KeyframeData? currentKeyframe = GetCurrentKeyframe();
        if (currentKeyframe == null) return;

        objectSwitcher.SetModel(objectSwitcher.GetMeshNames().IndexOf(currentKeyframe.MeshName));
        objectSwitcher.MoveModels(currentKeyframe.Position);
        objectSwitcher.SetRotation(currentKeyframe.Rotation);
        objectSwitcher.SetScale(currentKeyframe.Scale);
        objectSwitcher.ChangeMaterialColor(0, currentKeyframe.MaterialColor);

        GD.Print($"Applied Keyframe at {currentKeyframe.Time}s!");
    }

    public KeyframeData? GetCurrentKeyframe()
    {
        if (keyframes.Count == 0) return null;

        // 🔹 Find the closest keyframe to `currentTime`
        KeyframeData closestKeyframe = keyframes[0];
        float closestTimeDifference = Mathf.Abs(closestKeyframe.Time - currentTime);

        foreach (var keyframe in keyframes)
        {
            float timeDifference = Mathf.Abs(keyframe.Time - currentTime);
            if (timeDifference < closestTimeDifference)
            {
                closestKeyframe = keyframe;
                closestTimeDifference = timeDifference;
            }
        }

        return closestKeyframe;
    }

    public void RemoveKeyframeAt(int index)
    {
        if (index < 0 || index >= keyframes.Count) return;

        keyframes.RemoveAt(index);
        GD.Print($"[DEBUG] Keyframe {index} removed.");

        if (keyframes.Count == 0)
        {
            currentTime = 0;  // 🔹 Reset time if no keyframes remain
            isPlaying = false;
        }
        else
        {
            // 🔹 Snap to the closest keyframe after deletion
            SetTime(currentTime);
        }
    }
    


    public float GetCurrentTime() => currentTime;
    public List<KeyframeData> GetKeyframes() => keyframes;
}
