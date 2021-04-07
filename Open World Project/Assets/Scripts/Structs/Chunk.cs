using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.IO;

[Serializable]
public class Chunk
{
    public Vector3 chunk_pos;
    public int chunk_ID;

    public List<Basic> basic_objects;
    public List<TransformData> entity_references;

    public Bounds chunk_bounds;
    public List<int> chunk_neighbours;

    //Instantiate a Chunk in the world
    public void Load()
    {
        foreach (Basic obj in basic_objects)
        {
            obj.Spawn();
            ObjManager.LoadObject(ChunkManager.GetChunks(), obj);
        }

        foreach (TransformData ent in entity_references)
        {
            EntityManager.SpawnEntity(ent);
        }

        ChunkManager.GetChunks().chunks.Add(this);
    }

    //Destroy a Chunk in the world
    public void Unload()
    {
        RefreshChunkObjects(ChunkManager.GetWorldObjects());
        OverrideData();

        foreach (Basic basic in basic_objects)
        {
            Debug.Log(basic.transform_data.name);
            WorldManager.DeleteObject(basic.transform_data);
        }

        foreach (TransformData ent in entity_references)
        {
            EntityManager.Despawn(ent);
        }

        ChunkManager.GetChunks().chunks.Remove(this);
    }

    public void RefreshChunkObjects(Dictionary<GameObject, ObjectTypes> world_objects)
    {
        //ThreadManager.StartThreadedFunction(() => { FindWorldObjects(wo, eh); });

        foreach (KeyValuePair<GameObject, ObjectTypes> wo in world_objects)
        {
            if (chunk_bounds.Contains(wo.Key.transform.position))
            {
                if (!ChunksHaveObject(wo.Key))
                {
                    InitialseObj(wo.Value, wo.Key);
                }
                else
                {
                    continue;
                }
            }
        }
    }

    public bool ChunksHaveObject(GameObject go)
    {
        foreach (Basic obj in GetObjects())
        {
            if (obj.transform_data.runtime_ref == go)
            {
                return true;
            }
        }
        return false;
    }

    public List<Basic> GetObjects()
    {
        return basic_objects;
    }

    public void OverrideData()
    {
        string path = Application.dataPath + "/Resources/World Data/Chunks/Chunk" + chunk_ID + " Data.json";

        string json = JsonUtility.ToJson(this);

        File.WriteAllText(path, json);
    }

    public void InitialseObj(ObjectTypes type, GameObject go)
    {
        switch (type)
        {
            case ObjectTypes.BASIC:
                {
                    Basic basic = new Basic(this, go, true);
                    break;
                }
            case ObjectTypes.ENTITY:
                {
                    entity_references.Add(EntityManager.AddEntity(this, go));
                    break;
                }
            default:
                {
                    break;
                }
        }
    }

    public void ResetObjects()
    {
        basic_objects = new List<Basic>();
        entity_references = new List<TransformData>();
    }
}


[Serializable]
public struct ChunkData
{
    public Bounds detection_bounds;

    public Chunk current_chunk;
    public float chunk_size;

    public string directory;
    public List<Chunk> chunks;

    public List<Basic> pending_childs;

    public List<Basic> world_objects;
}
