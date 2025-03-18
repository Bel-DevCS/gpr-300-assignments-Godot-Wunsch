using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class A4_AnimatedObject : Node3D
{
    [Export] private A4_ObjectSwitcher objectSwitcher;
    [Export] private CanvasLayer ui;
    [Export] private Control uiControl;
    [Export] private A4_SwapParticle particleEffect;

    private List<KeyframeData> keyframes = new();
    private float currentTime = 0f;
    private bool isPlaying = false;
    private int easingType = 2; 
    private int originalModelIndex = -1;

    
    [Export] private bool loop = false;
    [Export] private float playbackSpeed = 1.0f; 

    public CanvasLayer GetUI() => ui;
    public Control GetControl() => uiControl;
    public A4_ObjectSwitcher GetObjectSwitcher() => objectSwitcher;
    
    public int GetEasingType() => easingType;

    public override void _Ready()
    {
        SetUIVisibility(false);
    }

    public override void _Process(double delta)
    {
        if (!isPlaying) return;

        currentTime += (float)delta * playbackSpeed; // Apply playback speed

        if (keyframes.Count > 0)
        {
            float lastKeyframeTime = keyframes[keyframes.Count - 1].Time;

            if (currentTime > lastKeyframeTime)
            {
                if (loop)
                {
                    currentTime = keyframes[0].Time; // Restart from beginning
                }
                else
                {
                    isPlaying = false;
                    currentTime = lastKeyframeTime;
                }
            }
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
            currentMeshName
        );

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

    public void PlayAnimation()
    {
        if (keyframes.Count < 2)
        {
            GD.PrintErr("Not enough keyframes to play animation!");
            return;
        }

        if (originalModelIndex == -1)
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
            originalModelIndex = -1;
        }
    }

    public void ResetAnimation()
    {
        isPlaying = false;
        currentTime = keyframes.Count > 0 ? keyframes[0].Time : 0f;
        ApplyInterpolatedFrame(currentTime);
        GD.Print("Animation Reset.");
    }

    public void SetPlaybackSpeed(float speed)
    {
        playbackSpeed = Mathf.Max(0.1f, speed); // Prevent 0x or negative speeds
        GD.Print($"Playback Speed Set to: {playbackSpeed}x");
    }

    public void SetLoop(bool enable)
    {
        loop = enable;
        GD.Print($"Looping Set to: {loop}");
    }

    public void SetTime(float newTime)
    {
        if (keyframes.Count == 0) return;

        currentTime = newTime;

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

        if (previous.MeshName != next.MeshName)
        {
            if (t >= 0.4f && particleEffect != null)
            {
                particleEffect.TriggerEffect(objectSwitcher.GetCurrentModel().GlobalTransform.Origin);
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
            currentTime = 0;
            isPlaying = false;
        }
        else
        {
            SetTime(currentTime);
        }
    }

    public float GetCurrentTime() => currentTime;
    public List<KeyframeData> GetKeyframes() => keyframes;
}
