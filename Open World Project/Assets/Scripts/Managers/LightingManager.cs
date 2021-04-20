using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

[ExecuteInEditMode]
public class LightingManager : MonoBehaviour
{
    LightingPresets lighting_presets;

    [SerializeField] private Light directional_light;
    public int number_of_presets = 0;

    #region Editor Functions

    public LightingPresets GetPresets()
    {
        return lighting_presets;
    }

    public LightingPresets LoadPresets()
    {
        if (Resources.Load("World Data/Lighting/Presets") != null)
        {
            string path = File.ReadAllText(Application.dataPath + "/Resources/World Data/Lighting/Presets.json");
            return JsonUtility.FromJson<LightingPresets>(path);
        }
        else
        {
            return ResetPresets();
        }
    }
    public LightingPresets ResetPresets()
    {
        lighting_presets = new LightingPresets();
        lighting_presets.presets = new List<LightingPreset>();

        for (int i = 0; i < number_of_presets; i++)
        {
            LightingPreset lp = new LightingPreset();

            lp.ambient_color = new Gradient();
            lp.directional_color = new Gradient();
            lp.fog_color = new Gradient();

            lighting_presets.presets.Add(lp);
        }
        return lighting_presets;
    }

    public void SavePresets(LightingPresets lps)
    {
        lighting_presets = lps;

        if (Directory.Exists(Application.dataPath + "/Resources/World Data/Lighting"))
        {
            if (lighting_presets.presets.Count > 0)
            {
                string json = JsonUtility.ToJson(lighting_presets);
                File.WriteAllText(Application.dataPath + "/Resources/World Data/Lighting/Presets.json", json);
#if UNITY_EDITOR
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
#endif
            }
        }
    }

    #endregion

    #region Runtime Fuctions

    public void LoadLightingPresets()
    {
        if (Resources.Load("World Data/Lighting/Presets") != null)
        {
            lighting_presets = new LightingPresets();

            string path = File.ReadAllText(Application.dataPath + "/Resources/World Data/Lighting/Presets.json");
            lighting_presets = JsonUtility.FromJson<LightingPresets>(path);

            if (lighting_presets.current_preset.ambient_color == null)
            {
                lighting_presets.current_preset = lighting_presets.presets[0];
            }
        }
    }

    public void UpdateLighting(float time_percent)
    {
        if (lighting_presets.presets == null)
        {
            LoadLightingPresets();
        }

        RenderSettings.ambientLight = lighting_presets.current_preset.ambient_color.Evaluate(time_percent);
        RenderSettings.fogColor = lighting_presets.current_preset.fog_color.Evaluate(time_percent);

        if (directional_light != null)
        {
            directional_light.color = lighting_presets.current_preset.directional_color.Evaluate(time_percent);
            directional_light.transform.localRotation = Quaternion.Euler(new Vector3((time_percent * 360.0f) - 90f, -170, 0));
        }
    }

    #endregion

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
