using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct LightingPreset
{
    public World.TimeState time_period;
    public Gradient ambient_color;
    public Gradient directional_color;
    public Gradient fog_color;
}
