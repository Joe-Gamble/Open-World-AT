using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

[ExecuteInEditMode]
public class LightingManager : MonoBehaviour
{

    [SerializeField] private Light directional_light;

    public LightingPreset[] presets;
    private LightingPreset current_preset;

    [SerializeField, Range(0, 24)] private float time_of_day;

    private void Update()
    {
        if (presets == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            time_of_day += Time.deltaTime / 10;
            time_of_day %= 24;
            UpdateLighting((time_of_day / 24f));
        }
        else
        {
            UpdateLighting(time_of_day / 24f);
        }

    }

    public void UpdateLightingPresets()
    {
        Debug.Log("Happening");

        if (Resources.Load("World Data/World Presets") != null)
        {
            string path = File.ReadAllText(Application.dataPath + "/Resources/World Data/World Presets.json");

            presets = JsonUtility.FromJson<WorldPresets>(path).lighting_presets.ToArray();
            current_preset = presets[0];
        }
    }

    private void UpdateLighting(float time_percent)
    {
        float splits = 24 / presets.Length;
        //int i = 0;

        current_preset = presets[0];
        /*
        foreach (LightingPreset lp in presets)
        {
            if (IsBetween(time_percent, splits * (float)presets[0].time_period, splits * (float)presets[i + 1].time_period))
            {
                current_preset = lp;
            }
            else
            {
                
            }
        }
        */
        RenderSettings.ambientLight = current_preset.ambient_color.Evaluate(time_percent);
        RenderSettings.fogColor = current_preset.fog_color.Evaluate(time_percent);

        if (directional_light != null)
        {
            directional_light.color = current_preset.directional_color.Evaluate(time_percent);
            directional_light.transform.localRotation = Quaternion.Euler(new Vector3((time_percent * 360.0f) - 90f, -170, 0));
        }
    }

    private void OnValidate()
    {
        if ((directional_light != null))
        {
            return;
        }
        if (RenderSettings.sun != null)
        {
            directional_light = RenderSettings.sun;
        }
        else
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    directional_light = light;
                    return;
                }
            }
        }
    }

    private bool IsBetween(float testValue, float bound1, float bound2)
    {
        return (testValue >= Mathf.Min(bound1, bound2) && testValue <= Mathf.Max(bound1, bound2));
    }
}
