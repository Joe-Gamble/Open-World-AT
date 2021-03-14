using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

//To do tomorrow
/*

-- player movement and chunk logic
-- Day/Night Cycle
-- Weather
--More chunk functionality to save objects/chunks?

*/

[ExecuteInEditMode]
public class ChunkManager : MonoBehaviour
{
    public GameObject testPlane;

    private float length;
    public int divides = 3;

    private float chunk_size;

    private List<Chunk> chunks;

    private Chunk current_chunk;

    private List<GameObject> world_objects;

    void Start()
    {

    }

    #region RunTime Functions

    //Create a Chunk from Json Data
    private Chunk GetChunkFromFile(int chunk_id)
    {
        string path = Application.dataPath + "/Resources/World Data/Chunks/Chunk" + chunk_id + "/Chunk Data.json";

        string path2 = File.ReadAllText(Application.dataPath + "/Resources/World Data/Chunks/Chunk" + chunk_id + "/Chunk Data.json");

        Chunk chunk = new Chunk();
        chunk = JsonUtility.FromJson<Chunk>(path2);

        return chunk;
    }

    public void InitialiseSpawnChunks(int chunk_id)
    {
        chunks = new List<Chunk>();

        SetCurrentChunk(GetChunkFromFile(chunk_id));
        LoadChunk(current_chunk);

        foreach (int neighbor in current_chunk.chunk_neighbours)
        {
            LoadChunk(GetChunkFromFile(neighbor));
        }
    }

    //Logic to Load/Unlod the correct chunks on entering a new one
    public void EnterNewChunk(Chunk new_chunk)
    {
        Debug.Log("Player has moved from Chunk " + current_chunk.chunk_ID + " to Chunk" + new_chunk.chunk_ID);
        List<int> needed_neighbours = new List<int>();

        foreach (int old_chunk_id in current_chunk.chunk_neighbours)
        {
            if (new_chunk.chunk_neighbours.Exists(x => x == old_chunk_id))
            {
                needed_neighbours.Add(old_chunk_id);
            }
            else if (old_chunk_id == new_chunk.chunk_ID)
            {
                needed_neighbours.Add(current_chunk.chunk_ID);
            }
            else
            {
                UnloadChunk(GetChunk(old_chunk_id));
            }
        }

        foreach (int new_chunk_neighbor_id in new_chunk.chunk_neighbours)
        {
            if (!needed_neighbours.Contains(new_chunk_neighbor_id))
            {
                LoadChunk(GetChunkFromFile(new_chunk_neighbor_id));
            }
        }
        SetCurrentChunk(new_chunk);
    }

    //Instantiate a Chunk in the world
    public void LoadChunk(Chunk chunk)
    {
        for (int i = 0; i < chunk.objects.Count(); i++)
        {
            Obj obj = chunk.objects[i];

            GameObject go;

            if (obj.is_unity_primitive)
            {
                go = GameObject.CreatePrimitive(obj.primitive_type);
                go.GetComponent<MeshRenderer>().sharedMaterial = obj.obj_mats[0];
            }
            else
            {
                go = new GameObject();

                go.AddComponent<MeshFilter>().sharedMesh = Resources.Load<Mesh>("World Data/Meshes/" + obj.obj_mesh.name);
                go.AddComponent<MeshRenderer>().sharedMaterials = obj.obj_mats;
            }

            go.name = obj.name;

            go.GetComponent<Transform>().position = obj.obj_position;
            go.GetComponent<Transform>().rotation = obj.obj_rotation;
            go.GetComponent<Transform>().localScale = obj.obj_scale;

            obj.runtime_ref = go;
        }

        foreach (Obj obj in chunk.objects)
        {
            if (obj.obj_parent != null)
            {
                if (obj.obj_parent == "World Objects")
                {
                    obj.runtime_ref.transform.parent = GameObject.Find("World Objects").transform;
                }
                else
                {
                    foreach (Obj p_obj in chunk.objects)
                    {
                        if (p_obj.name == obj.obj_parent)
                        {
                            obj.runtime_ref.transform.parent = p_obj.runtime_ref.transform;
                        }
                    }
                }

            }
        }
        chunks.Add(chunk);
    }

    //Destroy a Chunk in the world
    public void UnloadChunk(Chunk chunk)
    {
        foreach (Obj obj in chunk.objects)
        {
            Destroy(obj.runtime_ref);
            obj.runtime_ref = null;
        }

        chunks.Remove(chunk);
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
        chunk_size = length / divides;

        chunks = new List<Chunk>();

        Vector3 pos = new Vector3(((0 - length / 2) + (chunk_size / 2)), testPlane.transform.position.y, ((0 - length / 2) + (chunk_size / 2)));
        float reset = pos.x;

        for (int i = 1; i <= chunk_numbers; i++)
        {
            // i = 1-9
            Chunk chunk = new Chunk();
            chunk.chunk_pos = pos;

            chunk.chunk_ID = i;

            chunk.chunk_bounds = new Bounds(chunk.chunk_pos, new Vector3(chunk_size, chunk_size, chunk_size));

            chunk.objects = new List<Obj>();

            chunk.chunk_neighbours = new List<int>();

            //Grab all Gameobjects for mesh extraction

            string chunk_directory = "Chunk" + chunk.chunk_ID;
            AssetDatabase.CreateFolder("Assets/Resources/World Data/Chunks", chunk_directory);

            chunk.directory = "/Resources/World Data/Chunks/" + chunk_directory;

            chunks.Add(chunk);

            if (i % divides == 0)
            {
                pos.z += chunk_size;
                pos.x = reset;
            }
            else
            {
                pos.x += chunk_size;
            }
        }
        GenerateChunkNeighbours();
        current_chunk = chunks[3];
    }

    public void RemoveChunks()
    {
        chunks.Clear();
        chunks = null;
    }

    public void CollectData()
    {
        for (int i = 0; i < world_objects.Count; i++)
        {
            foreach (Chunk chunk in chunks)
            {
                if (chunk.chunk_bounds.Contains(world_objects[i].gameObject.transform.position))
                {
                    SaveWorldData(InitialseObj(chunk, world_objects[i].gameObject));
                    Debug.Log("Added " + world_objects[i].name + " to Chunk " + chunk.chunk_ID);
                    DestroyImmediate(world_objects[i].gameObject);
                    break;
                }
            }
        }
        SaveChunkData();
    }

    public void SaveWorldData(Obj go)
    {
        string m_path = "Assets/Resources/World Data/";

        //Materials
        if (go.obj_mats != null)
        {
            foreach (Material mat in go.obj_mats)
            {
                if (Resources.Load("World Data/Materials/" + mat.name) == null)
                {
                    AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(mat), m_path + "Materials/" + mat.name + ".mat");
                }
            }
        }

        //Meshes
        if (go.obj_mesh != null)
        {
            if (Resources.Load("World Data/Meshes/" + go.obj_mesh.name) == null)
            {
                string status = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(go.obj_mesh), m_path + "Meshes/" + go.obj_mesh.name + ".fbx");

                if (status != null)
                {
                    go.is_unity_primitive = true;

                    switch (go.obj_mesh.name)
                    {
                        case "Cube":
                            {
                                go.primitive_type = PrimitiveType.Cube;
                                break;
                            }
                        case "Sphere":
                            {
                                go.primitive_type = PrimitiveType.Sphere;
                                break;
                            }
                        case "Capsule":
                            {
                                go.primitive_type = PrimitiveType.Capsule;
                                break;
                            }
                        case "Plane":
                            {
                                go.primitive_type = PrimitiveType.Plane;
                                break;
                            }
                        case "Cylinder":
                            {
                                go.primitive_type = PrimitiveType.Cylinder;
                                break;
                            }
                        case "Quad":
                            {
                                go.primitive_type = PrimitiveType.Quad;
                                break;
                            }
                        default:
                            {
                                go.primitive_type = PrimitiveType.Cube;
                                break;
                            }
                    }
                }
            }
        }
        AssetDatabase.Refresh();
    }

    public void SaveChunkData()
    {
        foreach (Chunk chunk in chunks)
        {
            if (Directory.Exists(Application.dataPath + chunk.directory))
            {
                string json = JsonUtility.ToJson(chunk);
                File.WriteAllText(Application.dataPath + chunk.directory + "/Chunk Data.json", json);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

    }

    #endregion

    #region inactive code
    /*
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

    public bool HasChunks()
    {
        return !(chunks == null);
    }

    private void GenerateChunkNeighbours()
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            Chunk chunk = chunks[i];

            float box_size = chunk_size * 2;

            Bounds box = new Bounds(chunk.chunk_pos, new Vector3(box_size, box_size, box_size));

            foreach (Chunk other_chunk in chunks)
            {
                if (box.Intersects(other_chunk.chunk_bounds) && chunk.chunk_ID != other_chunk.chunk_ID)
                {
                    chunk.chunk_neighbours.Add(other_chunk.chunk_ID);
                }
            }
        }
    }

    public void FindWorldObjects()
    {
        world_objects = new List<GameObject>();

        GameObject host = GameObject.FindGameObjectWithTag("World Objects");
        Transform[] objects = new Transform[host.transform.childCount];

        for (int i = 0; i < host.transform.childCount; i++)
        {
            objects[i] = host.transform.GetChild(i);
            world_objects.Add(objects[i].gameObject);
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

    public Obj InitialseObj(Chunk chunk, GameObject go)
    {
        Obj obj = new Obj();

        if (go.transform.parent != null)
        {
            if (obj.obj_parent == null)
            {
                obj.obj_parent = go.transform.parent.name;
            }
        }

        if (go.transform.childCount > 0)
        {
            obj.child_names = new List<string>();

            for (int i = 0; i < go.transform.childCount; i++)
            {
                Obj obj_child = InitialseObj(chunk, go.transform.GetChild(i).gameObject);
                obj.child_names.Add(obj_child.name);
                SaveWorldData(obj_child);
            }
        }



        if (go.GetComponent<MeshRenderer>() != null)
        {
            Debug.Log("Adding material/mesh");
            Material[] mats = go.GetComponent<MeshRenderer>().sharedMaterials;
            obj.obj_mats = mats;
            obj.obj_mesh = go.GetComponent<MeshFilter>().sharedMesh;
        }

        Transform trans = go.GetComponent<Transform>();

        obj.name = trans.name;

        obj.obj_position = trans.position;
        obj.obj_rotation = trans.rotation;
        obj.obj_scale = trans.localScale;

        chunk.objects.Add(obj);
        return obj;
    }

    public List<Chunk> GetChunks()
    {
        return chunks;
    }

    public Chunk GetCurrentChunk()
    {
        return current_chunk;
    }

    public void SetCurrentChunk(Chunk chunk)
    {
        current_chunk = chunk;
    }

    public Chunk GetChunk(int chunk_id)
    {
        foreach (Chunk chunk in chunks)
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
        foreach (Chunk chunk in chunks)
        {
            if (chunk.chunk_bounds.Contains(pos))
            {
                return chunk;
            }
        }
        Debug.LogError("This Location: " + pos + " does not reside in any chunk");
        return new Chunk();
    }

    private void OnDrawGizmos()
    {
        if (HasChunks())
        {
            foreach (Chunk chunk in chunks)
            {
                Gizmos.DrawWireCube(chunk.chunk_pos, new Vector3(chunk_size, chunk_size, chunk_size));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    #endregion
}

[ExecuteInEditMode]
public class ChunkManagerEditor : EditorWindow
{
    public ChunkManager chunkManager;
    public GameObject chunkManagerGO;

    private void OnEnable()
    {
        chunkManagerGO = GameObject.FindGameObjectWithTag("GameController");
        chunkManager = chunkManagerGO.GetComponent<ChunkManager>();
    }

    [MenuItem("Window/ChunkEditor")]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        EditorWindow.GetWindow(typeof(ChunkManagerEditor));
    }

    void OnInspectorUpdate()
    {
        //Repaint();
    }

    public void OnGUI()
    {
        chunkManagerGO = (GameObject)EditorGUILayout.ObjectField("GameManager", chunkManagerGO, typeof(GameObject), true);

        if (GUILayout.Button("Generate Object List"))
        {
            chunkManager.FindWorldObjects();
        }

        if (GUILayout.Button("Generate Chunks"))
        {
            chunkManager = chunkManagerGO.GetComponent<ChunkManager>();

            chunkManager.UpdateDirectories();
            if (chunkManager.HasChunks())
            {
                chunkManager.RemoveChunks();
            }
            chunkManager.MakeChunks();
        }

        if (chunkManager.HasChunks())
        {
            if (GUILayout.Button("Delete Chunks"))
            {
                chunkManager.UpdateDirectories();
                chunkManager.RemoveChunks();
            }

            if (GUILayout.Button("Collect World Data"))
            {
                chunkManager.CollectData();
            }
        }
    }
}
