using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using System;

namespace World
{
    public enum TimeState
    {
        Sunrise,
        Morning,
        Afternoon,
        Dusk,
        Evening,
        Night
    }

    public enum SeasonState
    {
        Autumn,
        Winter,
        Spring,
        Summer
    }

    public enum WeatherState
    {
        Clear,
        Rain,
        Snow
    }
}

[Serializable]
public struct WorldPresets
{
    public List<LightingPreset> lighting_presets;
}

public class WorldManager : MonoBehaviour
{
    private ChunkManager chunkManager;
    private LightingManager lighting_manager;

    private World.SeasonState current_season;
    private World.TimeState current_time;

    public WorldPresets data;
    public int number_of_presets = 0;

    private GameObject player;

    private int spawn_chunk = 4;

    // Start is called before the first frame update
    void Start()
    {
        chunkManager = this.GetComponent<ChunkManager>();
        lighting_manager = this.GetComponent<LightingManager>();

        player = GameObject.FindGameObjectWithTag("Player");
        Initialise();
    }

    void Initialise()
    {
        chunkManager.InitialiseSpawnChunks(spawn_chunk);
        player.transform.position = chunkManager.GetChunk(spawn_chunk).chunk_pos;
    }

    // Update is called once per frame
    void Update()
    {
        if (!chunkManager.GetCurrentChunk().chunk_bounds.Contains(player.transform.position))
        {
            foreach (int neighbour in chunkManager.GetCurrentChunk().chunk_neighbours)
            {
                if (chunkManager.GetChunk(neighbour).chunk_bounds.Contains(player.transform.position))
                {
                    chunkManager.EnterNewChunk(chunkManager.GetChunk(neighbour));
                    break;
                }
            }
        }
    }
    public WorldPresets GetPresets()
    {
        if (Resources.Load("World Data/World Presets") != null)
        {
            //string path = File.ReadAllText(Application.dataPath + "/Resources/World Data/World Presets.json");
            //data = JsonUtility.FromJson<WorldPresets>(path);
            return data;
        }
        else
        {
            return ResetPresets();
        }
    }
    public WorldPresets ResetPresets()
    {
        data = new WorldPresets();
        data.lighting_presets = new List<LightingPreset>();

        for (int i = 0; i < number_of_presets; i++)
        {
            LightingPreset lp = new LightingPreset();

            lp.ambient_color = new Gradient();
            lp.directional_color = new Gradient();
            lp.fog_color = new Gradient();

            data.lighting_presets.Add(lp);
        }
        return data;
    }

    public void SavePresets()
    {
        if (Directory.Exists(Application.dataPath + "/Resources/World Data/"))
        {
            if (data.lighting_presets.Count > 0)
            {
                string json = JsonUtility.ToJson(data);
                File.WriteAllText(Application.dataPath + "/Resources/World Data/World Presets.json", json);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                lighting_manager = this.GetComponent<LightingManager>();

                lighting_manager.UpdateLightingPresets();
            }
        }
    }
}

#region World Editor Window

[ExecuteInEditMode, Serializable]
public class WorldManagerEditor : EditorWindow
{
    Vector2 scroll_pos = Vector2.zero;
    public GameObject world_manager_instance;

    private ChunkManager chunk_manager;
    private LightingManager lighting_manager;
    private WorldManager world_manager;



    private void OnEnable()
    {
        LinkScripts();
    }

    private void LinkScripts()
    {
        if (world_manager_instance != null)
        {
            chunk_manager = world_manager_instance.GetComponent<ChunkManager>();
            lighting_manager = world_manager_instance.GetComponent<LightingManager>();
            world_manager = world_manager_instance.GetComponent<WorldManager>();
        }
    }

    [MenuItem("Window/ChunkEditor")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(WorldManagerEditor));
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    public void OnGUI()
    {
        scroll_pos = GUILayout.BeginScrollView(scroll_pos, true, true, GUILayout.Width(position.width), GUILayout.Height(position.height));

        world_manager_instance = (GameObject)EditorGUILayout.ObjectField("World Manager", world_manager_instance, typeof(GameObject), true);

        LinkScripts();

        #region Chunk Tools

        EditorGUILayout.LabelField("Chunk Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Chunks"))
        {
            chunk_manager.UpdateDirectories();

            chunk_manager.FindWorldObjects();

            if (chunk_manager.HasChunks())
            {
                chunk_manager.RemoveChunks();
            }
            chunk_manager.MakeChunks();
        }

        if (chunk_manager.HasChunks())
        {
            if (GUILayout.Button("Delete Chunks"))
            {
                //chunkManager.RespawnObjectsFromJson();
                chunk_manager.UpdateDirectories();
                chunk_manager.RemoveChunks();
            }

            if (GUILayout.Button("Collect World Data"))
            {
                chunk_manager.CollectData();
            }
        }

        #endregion

        EditorGUILayout.LabelField("Lighting Settings", EditorStyles.boldLabel);

        world_manager.number_of_presets = EditorGUILayout.IntField(world_manager.number_of_presets);

        for (int i = 0; i < world_manager.GetPresets().lighting_presets.Count; i++)
        {
            LightingPreset lp = world_manager.GetPresets().lighting_presets[i];

            lp.time_period = (World.TimeState)EditorGUILayout.EnumFlagsField("Time Period", lp.time_period);
            lp.ambient_color = EditorGUILayout.GradientField("Ambient Color", lp.ambient_color);
            lp.directional_color = EditorGUILayout.GradientField("Directional Color", lp.directional_color);
            lp.fog_color = EditorGUILayout.GradientField("Fog Color", lp.fog_color);
        }

        if (GUILayout.Button("Push Colors to File"))
        {
            world_manager.SavePresets();
        }

        if (GUILayout.Button("Reset Colors"))
        {
            world_manager.ResetPresets();
        }

        GUILayout.EndScrollView();
    }
}
#endregion
