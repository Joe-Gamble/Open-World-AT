using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

//To do tomorrow
/*
-- Day/Night Cycle
-- Weather
--More chunk functionality to save objects/chunks?
*/

[ExecuteInEditMode]
public class ChunkManager : MonoBehaviour
{
    ChunkData chunk_data;

    public GameObject testPlane;

    private float length;
    public int divides = 3;

    private List<Obj> pending_childs = new List<Obj>();

    void Start()
    {

    }

    #region RunTime Functions

    //Create a Chunk from Json Data
    private Chunk GetChunkFromFile(int chunk_id)
    {
        string path = File.ReadAllText(Application.dataPath + "/Resources/World Data/Chunks/Chunk" + chunk_id + " Data.json");

        Chunk chunk = new Chunk();
        chunk = JsonUtility.FromJson<Chunk>(path);

        return chunk;
    }

    public void InitialiseSpawnChunks(int chunk_id)
    {
        ClearAllObjects();

        chunk_data = new ChunkData();
        chunk_data.chunks = new List<Chunk>();

        EnterNewChunk(true, GetChunkFromFile(chunk_id));
    }

    //Logic to Load/Unlod the correct chunks on entering a new one
    public void EnterNewChunk(bool spawning, Chunk spawn_chunk)
    {
        if (spawning)
        {
            LoadChunk(spawn_chunk);

            foreach (int neighbor in spawn_chunk.chunk_neighbours)
            {
                LoadChunk(GetChunkFromFile(neighbor));
            }
        }
        else
        {
            List<int> needed_neighbours = new List<int>();

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
                    UnloadChunk(GetChunk(old_chunk_id));
                }
            }

            foreach (int new_chunk_neighbor_id in spawn_chunk.chunk_neighbours)
            {
                if (!needed_neighbours.Contains(new_chunk_neighbor_id))
                {
                    LoadChunk(GetChunkFromFile(new_chunk_neighbor_id));
                }
            }
        }

        LinkChildren();
        SetCurrentChunk(spawn_chunk);
    }

    //Instantiate a Chunk in the world
    public void LoadChunk(Chunk chunk)
    {
        Debug.Log(chunk.chunk_ID + chunk.GetObjects().Count);

        foreach (Obj obj in chunk.GetObjects())
        {
            obj.SpawnObject();

            if (obj.obj_parent != null)
            {
                pending_childs.Add(obj);
            }
        }
        chunk_data.chunks.Add(chunk);
    }


    //Destroy a Chunk in the world
    public void UnloadChunk(Chunk chunk)
    {
        RefreshChunkObjects(chunk);

        if (chunk.GetObjects().Count > 0)
        {
            Debug.Log("Chunk " + chunk.chunk_ID + "has " + chunk.GetObjects().Count + "objects");

            chunk.OverrideData();

            foreach (Obj obj in chunk.GetObjects())
            {
                Destroy(obj.runtime_ref);
                obj.runtime_ref = null;
            }
        }
        chunk_data.chunks.Remove(chunk);
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

        pending_childs = new List<Obj>();

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
        chunk_data.chunks.Clear();
        chunk_data.chunks = new List<Chunk>();
    }

    public void CollectData()
    {

        foreach (KeyValuePair<GameObject, ObjectTypes> wo in FindWorldObjects())
        {
            GetChunkAtLoc(wo.Key.transform.position).InitialseObj(wo.Value, wo.Key);
        }

        ClearAllObjects();
        SaveAllChunkData();
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

            pending_childs = new List<Obj>();

            foreach (FileInfo fo in assetsInfo)
            {
                LoadChunk(GetChunkFromFile(i));
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

    private void LinkChildren()
    {
        foreach (Chunk chunk in chunk_data.chunks)
        {
            foreach (Obj obj in chunk.GetObjects())
            {
                if (obj.obj_parent == "World Objects")
                {
                    obj.runtime_ref.transform.parent = GameObject.Find("World Objects").transform;
                }
                else
                {
                    foreach (Obj other_obj in pending_childs)
                    {
                        if (obj.obj_parent == other_obj.name)
                        {
                            foreach (string name in other_obj.child_names)
                            {
                                if (name == obj.name)
                                {
                                    obj.runtime_ref.transform.parent = other_obj.runtime_ref.transform;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public bool IsDirectoryEmpty(string path)
    {
        return !Directory.EnumerateFileSystemEntries(path).Any();
    }

    public void RefreshChunkObjects(Chunk chunk)
    {
        //ThreadManager.StartThreadedFunction(() => { FindWorldObjects(wo, eh); });

        foreach (KeyValuePair<GameObject, ObjectTypes> wo in FindWorldObjects())
        {
            if (chunk.chunk_bounds.Contains(wo.Key.transform.position))
            {
                if (!ChunksHaveObject(wo.Key))
                {
                    chunk.InitialseObj(wo.Value, wo.Key);
                }
                else
                {
                    continue;
                }

            }
        }
    }

    public Dictionary<GameObject, ObjectTypes> FindWorldObjects()
    {
        Dictionary<GameObject, ObjectTypes> world_objects = new Dictionary<GameObject, ObjectTypes>();

        GameObject wo = GameObject.FindGameObjectWithTag("World Objects");
        ExtractChildrenFromObject(world_objects, ObjectTypes.BASIC, wo);

        GameObject we = GameObject.FindGameObjectWithTag("World Entities");
        ExtractChildrenFromObject(world_objects, ObjectTypes.ENTITY, we);

        return world_objects;
    }

    public bool ChunksHaveObject(GameObject go)
    {
        foreach (Chunk chunk in chunk_data.chunks)
        {
            foreach (Obj obj in chunk.GetObjects())
            {
                if (obj.runtime_ref == go)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool HasChunks()
    {
        return !(chunk_data.chunks == null);
    }

    public void ClearAllObjects()
    {
        foreach (KeyValuePair<GameObject, ObjectTypes> wo in FindWorldObjects())
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

    public void ExtractChildrenFromObject(Dictionary<GameObject, ObjectTypes> world_objects, ObjectTypes type, GameObject parent)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform obj = parent.transform.GetChild(i);

            world_objects.Add(obj.gameObject, type);

            if (obj.childCount > 0)
            {
                Transform[] children = obj.GetComponentsInChildren<Transform>();

                foreach (Transform child in children)
                {
                    ExtractChildrenFromObject(world_objects, type, child.gameObject);
                }
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

    public ChunkData GetChunks()
    {
        return chunk_data;
    }

    public Chunk GetCurrentChunk()
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

    public Chunk GetChunkAtLoc(Vector3 pos)
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

    private void OnDrawGizmos()
    {
        if (HasChunks())
        {
            float chunk_size = chunk_data.chunk_size;
            foreach (Chunk chunk in chunk_data.chunks)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(chunk.chunk_pos, new Vector3(chunk_size, 100, chunk_size));

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(chunk.chunk_pos, new Vector3(chunk_size / 3, chunk_size, chunk_size / 3));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    #endregion
}
