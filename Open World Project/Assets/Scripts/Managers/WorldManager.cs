using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

public class WorldManager : MonoBehaviour
{
    private ChunkManager chunkManager;

    private World.SeasonState current_season;
    private World.TimeState current_time;

    private GameObject player;

    private int spawn_chunk = 4;

    // Start is called before the first frame update
    void Start()
    {
        chunkManager = this.GetComponent<ChunkManager>();
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
