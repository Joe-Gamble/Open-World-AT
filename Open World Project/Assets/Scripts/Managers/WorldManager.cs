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

    public static void DeleteObject(TransformData data)
    {
        if (data.runtime_ref != null)
        {
            if (Application.isPlaying)
            {
                Destroy(data.runtime_ref);
            }
            else
            {
                DestroyImmediate(data.runtime_ref);
            }
            data.runtime_ref = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (ChunkManager.HasChunks())
        {
            ChunkData chunk_data = ChunkManager.GetChunks();
            float chunk_size = chunk_data.chunk_size;

            foreach (Chunk chunk in ChunkManager.GetChunks().chunks)
            {
                if (chunk == ChunkManager.GetCurrentChunk())
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(chunk.chunk_pos, new Vector3(chunk_size, 40, chunk_size));
                }
                else
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawCube(chunk.chunk_pos, new Vector3(chunk_size, 40, chunk_size));
                }
            }
        }
    }

    public void Initialise()
    {
        chunkManager = this.GetComponent<ChunkManager>();
        lighting_manager = this.GetComponent<LightingManager>();
        player = GameObject.FindGameObjectWithTag("Player");

        chunkManager.InitialiseSpawnChunks(spawn_chunk);
        lighting_manager.LoadLightingPresets();
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ChunkManager.SaveAllChunkData();
                Application.Quit();
            }
            #region Lighting

            time_of_day += Time.deltaTime / 10;
            time_of_day %= 24;
            lighting_manager.UpdateLighting(time_of_day / 24f);

            #endregion

            #region Chunks

            Chunk current_chunk = ChunkManager.GetCurrentChunk();
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
