using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;
using System;

namespace World
{
    public enum Region
    {
        Grasslands,
        Frozen_Peaks,
        Dusty_Dunes,
        Swamplands
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

public struct WorldData
{
    public ChunkData chunk_data;
    public LightingPresets lighting_presets;
}

public class WorldManager : MonoBehaviour
{
    public WorldData data;

    private ChunkManager chunkManager;
    private LightingManager lighting_manager;

    [SerializeField, Range(0, 24)] private float time_of_day;

    private World.SeasonState current_season;
    private World.Region current_region;

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
        lighting_manager.LoadLightingPresets();

        ExtractFromScripts();

        player.transform.position = chunkManager.GetChunk(spawn_chunk).chunk_pos;
    }

    void ExtractFromScripts()
    {
        data.chunk_data = chunkManager.GetChunks();
        data.lighting_presets = lighting_manager.GetPresets();
    }

    // Update is called once per frame
    void Update()
    {
        ExtractFromScripts();

        #region Lighting

        if (Application.isPlaying)
        {
            time_of_day += Time.deltaTime / 10;
            time_of_day %= 24;
            lighting_manager.UpdateLighting(time_of_day / 24f);
        }
        else
        {
            lighting_manager.UpdateLighting(time_of_day / 24f);
        }

        #endregion

        #region Chunks

        Chunk current_chunk = data.chunk_data.current_chunk;
        if (!current_chunk.chunk_bounds.Contains(player.transform.position))
        {
            foreach (int neighbour in current_chunk.chunk_neighbours)
            {
                if (chunkManager.GetChunk(neighbour).chunk_bounds.Contains(player.transform.position))
                {
                    Debug.Log(neighbour);

                    chunkManager.EnterNewChunk(chunkManager.GetChunk(neighbour));
                    break;
                }
            }
        }

        #endregion
    }
}

#region World Editor Window

[ExecuteInEditMode, Serializable]
public class WorldManagerEditor : EditorWindow
{
    Vector2 scroll_pos = Vector2.zero;
    public GameObject world_manager_instance;

    private WorldManager world_manager;
    private ChunkManager chunk_manager;
    private LightingManager lighting_manager;

    private WorldData data;

    private LightingPresets presets;

    private void OnEnable()
    {
        LinkScripts();
    }

    private void LinkScripts()
    {
        if (world_manager_instance != null)
        {
            if (world_manager == null)
            {
                world_manager = world_manager_instance.GetComponent<WorldManager>();
                data = world_manager.data;
            }
            if (chunk_manager == null)
            {
                chunk_manager = world_manager_instance.GetComponent<ChunkManager>();
            }
            if (lighting_manager == null)
            {
                lighting_manager = world_manager_instance.GetComponent<LightingManager>();
            }
        }
    }

    [MenuItem("Window/WorldEditor")]
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
            chunk_manager.MakeChunks(data.chunk_data);
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

        #region  Lighting Tools

        EditorGUILayout.LabelField("Lighting Settings", EditorStyles.boldLabel);

        lighting_manager.number_of_presets = EditorGUILayout.IntField(lighting_manager.number_of_presets);

        for (int i = 0; i < presets.presets.Count; i++)
        {
            LightingPreset lp = presets.presets[i];

            lp.region = (World.Region)EditorGUILayout.EnumFlagsField("Region", lp.region);
            lp.ambient_color = EditorGUILayout.GradientField("Ambient Color", lp.ambient_color);
            lp.directional_color = EditorGUILayout.GradientField("Directional Color", lp.directional_color);
            lp.fog_color = EditorGUILayout.GradientField("Fog Color", lp.fog_color);
        }

        if (GUILayout.Button("Read Current Colors"))
        {
            presets = lighting_manager.LoadPresets();
        }

        if (GUILayout.Button("Reset Colors"))
        {
            presets = lighting_manager.ResetPresets();
        }

        if (GUILayout.Button("Push Colors to File"))
        {
            lighting_manager.SavePresets(presets);
        }

        GUILayout.EndScrollView();

        #endregion
    }
}
#endregion
