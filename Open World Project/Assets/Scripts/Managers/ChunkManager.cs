using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

public class ChunkManager : MonoBehaviour
{
    private static ChunkData chunk_data;

    public GameObject testPlane;

    private float length;
    public int divides = 3;

    private static Dictionary<GameObject, ObjectTypes> world_objects = new Dictionary<GameObject, ObjectTypes>();

    void Start()
    {

    }

    #region RunTime Functions

    //Create a Chunk from Json Data
    public Chunk GetChunkFromFile(int chunk_id)
    {
        string path = File.ReadAllText(Application.dataPath + "/Resources/World Data/Chunks/Chunk" + chunk_id + " Data.json");

        Chunk chunk = new Chunk();
        chunk = JsonUtility.FromJson<Chunk>(path);

        return chunk;
    }

    public void InitialiseSpawnChunks(int chunk_id)
    {
        chunk_data = new ChunkData();
        chunk_data.chunks = new List<Chunk>();

        ClearAllObjects();

        EnterNewChunk(true, GetChunkFromFile(chunk_id));
    }

    //Logic to Load/Unlod the correct chunks on entering a new one
    public void EnterNewChunk(bool spawning, Chunk spawn_chunk)
    {
        FindWorldObjects();
        chunk_data.pending_childs = new List<Basic>();

        if (spawning)
        {
            spawn_chunk.Load();

            foreach (int neighbor_id in spawn_chunk.chunk_neighbours)
            {
                Chunk chunk = GetChunkFromFile(neighbor_id);
                chunk.Load();
            }
        }
        else
        {
            List<int> needed_neighbours = new List<int>();

            RefreshEntities();

            foreach (int old_chunk_id in chunk_data.current_chunk.chunk_neighbours)
            {
                if (spawn_chunk.chunk_neighbours.Exists(x => x == old_chunk_id))
                {
                    needed_neighbours.Add(old_chunk_id);
                }
                else if (old_chunk_id == spawn_chunk.chunk_ID)
                {
                    needed_neighbours.Add(chunk_data.current_chunk.chunk_ID);
                }
                else
                {
                    GetChunk(old_chunk_id).Unload();
                }
            }

            foreach (int new_chunk_neighbor_id in spawn_chunk.chunk_neighbours)
            {
                if (!needed_neighbours.Contains(new_chunk_neighbor_id))
                {
                    GetChunkFromFile(new_chunk_neighbor_id).Load();
                }
            }
        }
        LinkChildren();
        SetCurrentChunk(spawn_chunk);
    }

    #endregion

    #region Editor Functions

    public void MakeChunks()
    {
        length = testPlane.GetComponent<Renderer>().bounds.size.x;

        int chunk_numbers = divides * divides;
        //how many steps
        float chunk_offset_steps = divides * 2;
        //how big are the steps

        chunk_data = new ChunkData();
        chunk_data.chunks = new List<Chunk>();

        chunk_data.chunk_size = length / divides;

        chunk_data.directory = "/Resources/World Data/Chunks/";

        Vector3 plane_pos = testPlane.transform.position;

        Vector3 pos = new Vector3((plane_pos.x + (0 - length / 2) + (chunk_data.chunk_size / 2)), plane_pos.y, (plane_pos.z + (0 - length / 2) + (chunk_data.chunk_size / 2)));
        float reset = pos.x;

        for (int i = 1; i <= chunk_numbers; i++)
        {
            // i = 1-9
            Chunk chunk = new Chunk();
            chunk.chunk_pos = pos;

            chunk.chunk_ID = i;

            float size = chunk_data.chunk_size;

            chunk.chunk_bounds = new Bounds(chunk.chunk_pos, new Vector3(size, Mathf.Infinity, size));

            chunk.ResetObjects();

            chunk.chunk_neighbours = new List<int>();

            //Grab all Gameobjects for mesh extraction

            chunk_data.chunks.Add(chunk);

            if (i % divides == 0)
            {
                pos.z += chunk_data.chunk_size;
                pos.x = reset;
            }
            else
            {
                pos.x += chunk_data.chunk_size;
            }
        }
        GenerateChunkNeighbours();
    }

    public void RemoveChunks()
    {
        if (chunk_data.chunks != null)
        {
            chunk_data.chunks.Clear();
        }
        chunk_data.chunks = new List<Chunk>();
    }

    public void CollectData()
    {
        FindWorldObjects();

        foreach (KeyValuePair<GameObject, ObjectTypes> wo in world_objects)
        {
            GetChunkAtLoc(wo.Key.transform.position).InitialseObj(wo.Value, wo.Key);
        }

        EntityManager.OverrideEntityData();
        SaveAllChunkData();
        ClearAllObjects();
    }

    public void LoadChunksFromDisk()
    {
        string path = Application.dataPath + "/Resources/World Data/Chunks";
        if (!IsDirectoryEmpty(path))
        {
            ClearAllObjects();
            RemoveChunks();

            DirectoryInfo chunks_path = new DirectoryInfo(path);
            FileInfo[] assetsInfo = chunks_path.GetFiles("*.json", SearchOption.AllDirectories);

            int i = 1;

            foreach (FileInfo fo in assetsInfo)
            {
                Chunk chunk = GetChunkFromFile(i);
                chunk.Load();

                i++;
            }
            LinkChildren();
        }
        else
        {
            Debug.Log("No Chunks to Spawn");
            return;
        }
    }

    public void SaveAllChunkData()
    {
        foreach (Chunk chunk in chunk_data.chunks)
        {
            chunk.OverrideData();
        }
        AssetDatabase.Refresh();
    }
    #endregion

    #region inactive code
    /*

    if (obj.obj_parent == "World Objects")
                {
                    obj.runtime_ref.transform.parent = GameObject.Find("World Objects").transform;
                }
                else
                {
                    bool found_parent = false;
                    //This is the problem 
                    foreach (Obj p_obj in chunk.objects)
                    {
                        if (p_obj.name == obj.obj_parent)
                        {
                            obj.runtime_ref.transform.parent = p_obj.runtime_ref.transform;
                            found_parent = true;
                        }
                    }
                    if (!found_parent)
                    {
                        foreach (Chunk other_chunk in chunk_data.chunks)
                        {
                            foreach (Obj p_obj in other_chunk.objects)
                            {
                                if (p_obj.name == obj.obj_parent)
                                {
                                    obj.runtime_ref.transform.parent = p_obj.runtime_ref.transform;
                                    found_parent = true;
                                }
                            }
                        }
                    }
                    if (!found_parent)
                    {
                        pending_childs.Add(obj);
                    }
                }

    public void RespawnObjectsFromJson()
    {
        string[] dir = Directory.GetDirectories(Application.dataPath + "/Resources/World Data/Chunks");

        if (!Directory.EnumerateFileSystemEntries(dir[0]).Any())
        {
            return;
        }

        foreach (string s in dir)
        {
            string path = File.ReadAllText(s + "/Chunk Data.json");
            Chunk chunk = new Chunk();
            chunk = JsonUtility.FromJson<Chunk>(path);

            LoadChunk(chunk);
        }
    }

    public void SaveWorld()
    {
        foreach (Chunk chunk in chunks)
        {
            string chunkString = File.ReadAllText(Application.dataPath + chunk.directory + "/Chunk Data.json");

            Chunk save_chunk = JsonUtility.FromJson<Chunk>(chunkString);

            foreach (Obj chunk_object in save_chunk.objects)
            {
                SaveObject(chunk, chunk_object, true, true);
            }
        }
    }

    public static void SaveObject(Chunk chunk, Obj obj, bool makeNewInstance, bool optimizeMesh)
    {
        string path = chunk.directory + "/World Objects";

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        print(path);
        /*

        GameObject go = new GameObject();
        go.AddComponent<Transform>();

        Transform local_obj_transform = obj.obj_transform;
        go.transform.position = local_obj_transform.position;
        go.transform.rotation = local_obj_transform.rotation;
        go.transform.localScale = local_obj_transform.localScale;


        //refabUtility.SaveAsPrefabAssetAndConnect(mesh_filter.gameObject, ob_path, InteractionMode.UserAction);

        if (optimizeMesh)
        {
            //MeshUtility.Optimize(meshToSave);
        }

        string m_path = "Assets/" + path + "/" + obj.obj_mat.name + ".mat";


        //AssetDatabase.CreateAsset(obj.obj_mat, m_path);
        AssetDatabase.SaveAssets();
    }
    */
    #endregion

    #region HelperFunctions

    public bool IsDirectoryEmpty(string path)
    {
        return !Directory.EnumerateFileSystemEntries(path).Any();
    }

    public void FindWorldObjects()
    {
        world_objects = new Dictionary<GameObject, ObjectTypes>();

        GameObject wo = GameObject.FindGameObjectWithTag("World Objects");
        ExtractChildrenFromObject(ObjectTypes.BASIC, wo);

        GameObject we = GameObject.FindGameObjectWithTag("World Entities");
        ExtractChildrenFromObject(ObjectTypes.ENTITY, we);
    }

    private void RefreshEntities()
    {
        foreach (Chunk chunk in chunk_data.chunks)
        {
            foreach (TransformData ent in chunk.entity_references.ToList())
            {
                if (!chunk.chunk_bounds.Contains(ent.runtime_ref.transform.position))
                {
                    ent.position = ent.runtime_ref.transform.position;
                    ChunkManager.GetChunkAtLoc(ent.position).entity_references.Add(ent);

                    chunk.entity_references.Remove(ent);
                }
                else
                {
                    ent.position = ent.runtime_ref.transform.position;
                }
            }
        }
    }

    private void LinkChildren()
    {
        foreach (Basic obj in chunk_data.pending_childs)
        {
            TransformData trans = obj.transform_data;
            if (trans.parent == (int)ObjManager.StaticObjects.WORLD_OBJECTS)
            {
                obj.transform_data.runtime_ref.transform.parent = GameObject.Find("World Objects").transform;
            }
            else if (trans.parent == (int)ObjManager.StaticObjects.WORLD_OBJECTS)
            {
                obj.transform_data.runtime_ref.transform.parent = GameObject.Find("World Entities").transform;
            }
            else
            {
                Basic parent = ObjManager.FindObject(chunk_data.pending_childs, trans.parent);
                if (parent != null)
                {
                    obj.transform_data.runtime_ref.transform.parent = parent.transform_data.runtime_ref.transform;
                }
            }
        }
        chunk_data.pending_childs.Clear();
    }


    public static bool HasChunks()
    {
        return !(chunk_data.chunks == null);
    }

    public void ClearAllObjects()
    {
        chunk_data.pending_childs = new List<Basic>();
        chunk_data.world_objects = new List<Basic>();

        FindWorldObjects();
        foreach (KeyValuePair<GameObject, ObjectTypes> wo in world_objects)
        {
            DestroyImmediate(wo.Key);
        }
    }

    private void GenerateChunkNeighbours()
    {
        for (int i = 0; i < chunk_data.chunks.Count; i++)
        {
            Chunk chunk = chunk_data.chunks[i];

            float box_size = chunk_data.chunk_size * 2;

            Bounds box = new Bounds(chunk.chunk_pos, new Vector3(box_size, box_size, box_size));

            foreach (Chunk other_chunk in chunk_data.chunks)
            {
                if (box.Intersects(other_chunk.chunk_bounds) && chunk.chunk_ID != other_chunk.chunk_ID)
                {
                    chunk.chunk_neighbours.Add(other_chunk.chunk_ID);
                }
            }
        }
    }

    public static void ExtractChildrenFromObject(ObjectTypes type, GameObject parent)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform obj = parent.transform.GetChild(i);

            if (!world_objects.ContainsKey(obj.gameObject))
            {
                world_objects.Add(obj.gameObject, type);
            }
        }
    }

    public void UpdateDirectories()
    {
        string world_data_directory = Application.dataPath + "/Resources/World Data";

        if (Directory.Exists(world_data_directory))
        {
            FileUtil.DeleteFileOrDirectory(world_data_directory + "/Chunks");

            if (!Directory.Exists(world_data_directory + "/Materials"))
            {
                Directory.CreateDirectory(world_data_directory + "/Materials");
            }

            if (!Directory.Exists(world_data_directory + "/Meshes"))
            {
                Directory.CreateDirectory(world_data_directory + "/Meshes");
            }
            AssetDatabase.Refresh();
        }
        else
        {
            Directory.CreateDirectory(world_data_directory);
            Directory.CreateDirectory(world_data_directory + "/Chunks");

            if (!Directory.Exists(world_data_directory + "/Materials"))
            {
                Directory.CreateDirectory(world_data_directory + "/Materials");
            }

            if (!Directory.Exists(world_data_directory + "/Meshes"))
            {
                Directory.CreateDirectory(world_data_directory + "/Meshes");
            }
        }
    }

    public static ChunkData GetChunks()
    {
        return chunk_data;
    }

    public static Chunk GetCurrentChunk()
    {
        return chunk_data.current_chunk;
    }

    public void SetCurrentChunk(Chunk chunk)
    {
        chunk_data.current_chunk = chunk;
    }

    public Chunk GetChunk(int chunk_id)
    {
        foreach (Chunk chunk in chunk_data.chunks)
        {
            if (chunk.chunk_ID == chunk_id)
            {
                return chunk;
            }
        }
        return new Chunk();
    }

    public static Chunk GetChunkAtLoc(Vector3 pos)
    {
        foreach (Chunk chunk in chunk_data.chunks)
        {
            if (chunk.chunk_bounds.Contains(pos))
            {
                return chunk;
            }
        }
        Debug.LogError("This Location: " + pos + " does not reside in any chunk");
        return null;
    }

    public static Dictionary<GameObject, ObjectTypes> GetWorldObjects()
    {
        return world_objects;
    }

    // Update is called once per frame
    void Update()
    {

    }
    #endregion
}
