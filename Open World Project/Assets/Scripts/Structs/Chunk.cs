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
    public List<string> entity_references;

    public Bounds chunk_bounds;
    public List<int> chunk_neighbours;

    //Instantiate a Chunk in the world
    public void Load(ChunkManager cm)
    {
        foreach (Basic basic in basic_objects)
        {
            ObjManager.AddObject(basic, false);

            Obj obj = basic.obj_data;
            obj.SpawnObject();
            ChunkManager.GetChunks().pending_childs.Add(basic);
        }

        foreach (string ent in entity_references)
        {
            EntityManager.SpawnEntity(ent);
        }

        ChunkManager.GetChunks().chunks.Add(this);
    }

    //Destroy a Chunk in the world
    public void Unload(ChunkManager cm)
    {
        RefreshChunkObjects(ChunkManager.GetWorldObjects());
        OverrideData();

        foreach (Basic basic in basic_objects)
        {
            ObjManager.objs.Remove(basic);
            cm.DeleteObject(basic.obj_data);
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
        foreach (Obj obj in GetObjects())
        {
            if (obj.runtime_ref == go)
            {
                return true;
            }
        }
        return false;
    }

    public List<Obj> GetObjects()
    {
        List<Obj> objs = new List<Obj>();

        foreach (Basic bas in basic_objects)
        {
            objs.Add(bas.obj_data);
        }

        return objs;
    }

    public Obj FindObject(string name)
    {
        foreach (Obj obj in GetObjects())
        {
            if (obj.name == name)
            {
                return obj;
            }
        }
        return null;
    }

    public Obj FindObject(int id)
    {
        foreach (Obj obj in GetObjects())
        {
            if (obj.obj_id == id)
            {
                return obj;
            }
        }
        return null;
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
                    Basic basic = new Basic(this, go);
                    break;
                }
            case ObjectTypes.ENTITY:
                {
                    entity_references.Add(EntityManager.AddEntity(go));
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
        entity_references = new List<string>();
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
}
