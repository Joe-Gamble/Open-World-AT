using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct LightingPreset
{
    public World.Region region;
    public Gradient ambient_color;
    public Gradient directional_color;
    public Gradient fog_color;
}

[Serializable]
public struct LightingPresets
{
    public LightingPreset current_preset;
    public List<LightingPreset> presets;
}
