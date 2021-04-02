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

public class WorldManager : MonoBehaviour
{
    private ChunkManager chunkManager;
    private LightingManager lighting_manager;

    [SerializeField, Range(0, 24)] private float time_of_day;

    private World.SeasonState current_season;
    private World.Region current_region;

    public GameObject test;

    private GameObject player;

    private int spawn_chunk = 6;

    // Start is called before the first frame update
    void Start()
    {
        Initialise();
    }

    public void Initialise()
    {
        chunkManager = this.GetComponent<ChunkManager>();
        lighting_manager = this.GetComponent<LightingManager>();
        player = GameObject.FindGameObjectWithTag("Player");

        chunkManager.InitialiseSpawnChunks(spawn_chunk);
        lighting_manager.LoadLightingPresets();

        player.transform.position = chunkManager.GetChunk(spawn_chunk).chunk_pos;
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isPlaying)
        {
            #region Lighting

            time_of_day += Time.deltaTime / 10;
            time_of_day %= 24;
            lighting_manager.UpdateLighting(time_of_day / 24f);

            #endregion

            #region Chunks

            Chunk current_chunk = chunkManager.GetCurrentChunk();
            if (!current_chunk.chunk_bounds.Contains(player.transform.position))
            {
                foreach (int neighbour in current_chunk.chunk_neighbours)
                {
                    if (chunkManager.GetChunk(neighbour).chunk_bounds.Contains(player.transform.position))
                    {
                        chunkManager.EnterNewChunk(false, chunkManager.GetChunk(neighbour));
                        break;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                GameObject go = GameObject.Instantiate(test, player.transform.position, Quaternion.identity);
                go.transform.parent = GameObject.Find("World Objects").transform;
            }

            #endregion
        }
        else
        {
            lighting_manager.UpdateLighting(time_of_day / 24f);
        }
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

    private LightingPresets presets;

    private void OnEnable()
    {
        if (world_manager_instance == null)
        {
            world_manager_instance = GameObject.FindGameObjectWithTag("GameController");
            LinkScripts();
        }
    }

    private void LinkScripts()
    {
        if (world_manager_instance != null)
        {
            if (world_manager == null)
            {
                world_manager = world_manager_instance.GetComponent<WorldManager>();
            }
            if (chunk_manager == null)
            {
                chunk_manager = world_manager_instance.GetComponent<ChunkManager>();
            }
            if (lighting_manager == null)
            {
                lighting_manager = world_manager_instance.GetComponent<LightingManager>();
                presets = lighting_manager.GetPresets();
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

        #region Chunk Tools

        EditorGUILayout.LabelField("Chunk Tools", EditorStyles.boldLabel);

        int d = EditorGUILayout.IntField("Spilts", lighting_manager.number_of_presets);

        if (GUILayout.Button("Read Current Chunks"))
        {
            chunk_manager.LoadChunksFromDisk();
        }

        if (GUILayout.Button("Remove Objects from World"))
        {
            chunk_manager.ClearAllObjects();
        }


        if (GUILayout.Button("Generate Chunks"))
        {
            chunk_manager.divides = d;

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
                chunk_manager.UpdateDirectories();
                chunk_manager.RemoveChunks();
            }

            if (GUILayout.Button("Collect World Data"))
            {
                chunk_manager.UpdateDirectories();
                chunk_manager.CollectData();
            }
        }

        #endregion

        #region  Lighting Tools

        EditorGUILayout.LabelField("Lighting Settings", EditorStyles.boldLabel);

        lighting_manager.number_of_presets = EditorGUILayout.IntField("Number of Presets", lighting_manager.number_of_presets);

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
