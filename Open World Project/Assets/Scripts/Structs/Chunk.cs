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
    public List<Entity> entity_objects;

    public Bounds chunk_bounds;
    public List<int> chunk_neighbours;

    public List<Obj> GetObjects()
    {
        List<Obj> objs = new List<Obj>();

        foreach (Basic bas in basic_objects)
        {
            objs.Add(bas.basic_object);
        }

        foreach (Entity ent in entity_objects)
        {
            objs.Add(ent.entity_object);
        }

        return objs;
    }

    public void OverrideData()
    {
        string path = Application.dataPath + "/Resources/World Data/Chunks/Chunk" + chunk_ID + " Data.json";

        Debug.Log(this.basic_objects.Count);

        string json = JsonUtility.ToJson(this);

        File.WriteAllText(path, json);

        AssetDatabase.Refresh();
    }

    public void InitialseObj(ObjectTypes type, GameObject go)
    {
        switch (type)
        {
            case ObjectTypes.BASIC:
                {
                    Basic basic = new Basic(go);
                    basic_objects.Add(basic);
                    break;
                }
            case ObjectTypes.ENTITY:
                {
                    Entity entity = new Entity(go);
                    entity_objects.Add(entity);
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
        entity_objects = new List<Entity>();
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
}
