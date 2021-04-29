using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#region World Editor Window

[ExecuteInEditMode]
public class WorldManagerEditor : EditorWindow
{
    Vector2 scroll_pos = Vector2.zero;
    public GameObject world_manager_instance;

    private WorldManager world_manager;
    private ChunkManager chunk_manager;
    private LightingManager lighting_manager;

    private LightingPresets presets = new LightingPresets();

    private void OnEnable()
    {
        if (presets.presets == null)
        {
            presets = new LightingPresets();
            presets.presets = new List<LightingPreset>();
        }

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
            if (ChunkManager.HasChunks())
            {
                chunk_manager.RemoveChunks();
            }
            chunk_manager.MakeChunks();
        }

        if (ChunkManager.HasChunks())
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

        if (presets.presets != null)
        {
            if (presets.presets.Count > 0)
            {
                for (int i = 0; i < presets.presets.Count; i++)
                {
                    LightingPreset lp = presets.presets[i];

                    lp.region = (World.Region)EditorGUILayout.EnumFlagsField("Region", lp.region);
                    lp.ambient_color = EditorGUILayout.GradientField("Ambient Color", lp.ambient_color);
                    lp.directional_color = EditorGUILayout.GradientField("Directional Color", lp.directional_color);
                    lp.fog_color = EditorGUILayout.GradientField("Fog Color", lp.fog_color);
                }
            }
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
