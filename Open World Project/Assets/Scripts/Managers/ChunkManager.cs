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

    private List<GameObject> world_objects;
    private List<GameObject> world_entities;

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

    public void OverrideChunkData(Chunk chunk)
    {
        string path = Application.dataPath + "/Resources/World Data/Chunks/Chunk" + chunk.chunk_ID + " Data.json";
        string json = JsonUtility.ToJson(chunk);

        File.WriteAllText(path, json);

        AssetDatabase.Refresh();
    }

    //Logic to Load/Unlod the correct chunks on entering a new one
    public void EnterNewChunk(bool spawning, Chunk new_chunk)
    {
        if (spawning)
        {
            LoadChunk(new_chunk);

            foreach (int neighbor in new_chunk.chunk_neighbours)
            {
                LoadChunk(GetChunkFromFile(neighbor));
            }
        }
        else
        {
            List<int> needed_neighbours = new List<int>();

            foreach (int old_chunk_id in chunk_data.current_chunk.chunk_neighbours)
            {
                if (new_chunk.chunk_neighbours.Exists(x => x == old_chunk_id))
                {
                    needed_neighbours.Add(old_chunk_id);
                }
                else if (old_chunk_id == new_chunk.chunk_ID)
                {
                    needed_neighbours.Add(chunk_data.current_chunk.chunk_ID);
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
        }

        LinkChildren();
        SetCurrentChunk(new_chunk);
    }

    //Instantiate a Chunk in the world
    public void LoadChunk(Chunk chunk)
    {
        foreach (Obj obj in chunk.objects)
        {
            SpawnObject<Obj>(obj, obj.obj_type);

            if (obj.obj_parent != null)
            {
                pending_childs.Add(obj);
            }
        }
        foreach (Entity ent in chunk.entities)
        {
            SpawnObject<Entity>(ent, ent.obj_type);

            if (ent.obj_parent != null)
            {
                pending_childs.Add(ent);
            }
        }
        chunk_data.chunks.Add(chunk);
    }

    //Destroy a Chunk in the world
    public void UnloadChunk(Chunk chunk)
    {
        RefreshChunkObjects(chunk);

        if (chunk.objects.Count > 0)
        {
            Debug.Log("Chunk " + chunk.chunk_ID + "has " + chunk.objects.Count + "objects");

            OverrideChunkData(chunk);

            foreach (Obj obj in chunk.objects)
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

            chunk.chunk_bounds = new Bounds(chunk.chunk_pos, new Vector3(size, size, size));

            chunk.objects = new List<Obj>();
            chunk.entities = new List<Entity>();

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
        FindWorldObjects();

        foreach (GameObject go in world_objects)
        {
            InitialseObj(GetChunkAtLoc(go.transform.position), go);
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
            OverrideChunkData(chunk);
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

    private void SpawnObject<T>(T o, ObjectTypes type)
    {
        Obj obj;
        switch (type)
        {
            case ObjectTypes.BASIC:
                {
                    obj = o as Obj;
                    break;
                }
            case ObjectTypes.ENTITY:
                {
                    obj = o as Entity;
                    break;
                }
            default:
                {
                    obj = o as Obj;
                    break;
                }
        }
        GameObject go;
        List<Material> mats = new List<Material>();

        //Unity Primitive
        if (obj.is_unity_primitive)
        {
            go = GameObject.CreatePrimitive(obj.primitive_type);

            Material default_mat = go.GetComponent<MeshRenderer>().sharedMaterial;

            if (obj.obj_mats != null)
            {
                foreach (string mat_name in obj.obj_mats)
                {
                    if (mat_name == "Default-Material")
                    {
                        mats.Add(default_mat);
                    }
                    else
                    {
                        mats.Add(Resources.Load("World Data/Materials/" + mat_name, typeof(Material)) as Material);
                    }
                }
            }
        }

        //Non Unity Object
        else
        {
            go = new GameObject();

            if (obj.obj_mesh != "")
            {
                go.AddComponent<MeshFilter>().sharedMesh = Resources.Load<Mesh>("World Data/Meshes/" + obj.obj_mesh);
                go.AddComponent<MeshRenderer>();
            }

            if (obj.obj_mats != null)
            {
                foreach (string mat_name in obj.obj_mats)
                {
                    mats.Add(Resources.Load("World Data/Materials/" + mat_name, typeof(Material)) as Material);
                }
            }
        }

        go.TryGetComponent(out MeshRenderer mr);

        if (mr != null)
        {
            mr.sharedMaterials = mats.ToArray();
        }

        if (obj.collider_type != "")
        {
            switch (obj.collider_type)
            {
                case "UnityEngine.MeshCollider":
                    {
                        go.AddComponent<MeshCollider>();
                        break;
                    }
                case "UnityEngine.BoxCollider":
                    {
                        break;
                    }
                case "UnityEngine.SphereCollider":
                    {
                        break;
                    }
                case "UnityEngine.CapsuleCollider":
                    {
                        break;
                    }
            }
        }

        go.GetComponent<Transform>().position = obj.obj_position;
        go.GetComponent<Transform>().rotation = obj.obj_rotation;
        go.GetComponent<Transform>().localScale = obj.obj_scale;

        go.name = obj.name;
        obj.runtime_ref = go;

    }

    private void LinkChildren()
    {
        foreach (Chunk chunk in chunk_data.chunks)
        {
            foreach (Obj obj in chunk.objects)
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

            foreach (Entity ent in chunk.entities)
            {
                if (ent.obj_parent == "World Entities")
                {
                    ent.runtime_ref.transform.parent = GameObject.Find("World Entities").transform;
                }
                else
                {
                    foreach (Obj other_obj in pending_childs)
                    {
                        if (ent.obj_parent == other_obj.name)
                        {
                            foreach (string name in other_obj.child_names)
                            {
                                if (name == ent.name)
                                {
                                    ent.runtime_ref.transform.parent = other_obj.runtime_ref.transform;
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
        FindWorldObjects();

        foreach (GameObject go in world_objects)
        {
            if (chunk.chunk_bounds.Contains(go.transform.position))
            {
                if (!ChunksHaveObject(go))
                {
                    InitialseObj(chunk, go);
                }
                else
                {
                    continue;
                }

            }
        }
    }

    public void FindWorldObjects()
    {

            }
        
    public bool ChunksHaveObject(GameObject go)
    {
        foreach (Chunk chunk in chunk_data.chunks)
        {
            foreach (Obj obj in chunk.objects)
            {
                if (obj.runtime_ref == go)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void ExtractChildrenFromObject(Transform p_transform, Transform[] children)
    {
 
    {
                    world_objects.Add(child.gameObject);
        }
    }
    }

    {
        ic bool HasChunks()
    }



    {
        {
            Fin
        {
                oreach(GameObject go in world_objects)
        {

                    DestroyImmediate(go);
                }
            }

            {







                {

                    {
                        {
                        }
                    }
                                    }
    }

    public void UpdateDirectories()
    {
        string world_data_directory = Application.dataPath + "/Resources/World Data";

    {

                    FileUtil.DeleteFileOrDirectory(world_data_directory + "/Chunks");

            if (!Directory.Exists(world_data_directory + "/Materials"))
         
            {


            if (!Directory.Exists(world_data_directory + "/Meshes"))

                        Directory.CreateDirectory(world_data_directory + "/Meshes");
            }
            Asset


                {
            }
            Directory.CreateDirector
}
        else
        {


            Directory.CreateDirectory(world_data_directory);


            if (!Directory.Exists(world_data_directory + "/Materials"))
            {
                {
                    Directory.CreateDirectory(world_data_directory + "/Materials");
                }


                !Directory.Exists(world_data_directory + "/Meshes"))

                Directory.CreateDirectory(world_data_directory + "/Meshes");
Component<MeshFilter>().sharedMesh;

        foreach (Material mat in mats)
        {
            obj.obj_mats.Add(mat.name);
        }

        obj.obj_mesh = sm.name;

        string m_path = "Assets/Resources/World Data/";

     
        {
        }





        //Meshes
        //Meshes
        {
            {
                {
                    {


                        {
                            // print(status);
                                                    }


                            {
                                {
                                    {
                                        {
                                            break;
                                                                obj.primitive_type = PrimitiveType.Capsule;
                                break;
                         
                                            {
                                                break;
                                            }
                                        }
                                        {
                                            {
                                                break;
                                            }
                                        }
                                        {
                                            {
                                                break;
                                                                        obj.primitive_type = PrimitiveType.Quad;
                                break;
                         
                                                {
                                                    break;
                                                                                obj.primitive_type = PrimitiveType.Cube;
                                break;
                         
                                                    {
                                                        break;
                                                    }
                                                }
                                                
        //Materials
        if
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            //Materials
                            {

                                {
                                    //Debug.Log("Moving Asset");
                                    //Debug.Log("Moving Asset");
                                    {
                                        {
                                            //Debug.Log(status);
                                        }
                                    }
                                }


                                {

                                }


                                                        
                                                        {
                            }

                                                                        obj.obj_
                            {
                                                        }
                                                    

                                                                
                                {
                                    {
                                                                   
                                        {
                                        }
                                                                                    {
                obj.child_names.Add(child.name);
            }
        }

                                                        if (go.GetComponent<MeshRenderer>() != null)
        {
            SaveObjectData(obj, go);
        }

                                {
                                                
                                    {
                                    }
                                }
                            }
                        }


                                        
                        {


                        }


                                              

                                              
                                            obj.ob
                                                         



                                             
                        {
                                                }
                            {
                                                        
                                                
                                                    
                            
                                    TITY;
                            }
                            {
                                                            {
                                                   
                            }
        else
                            {
                                                            {
                                return;

                            }

                        
                            {
                                {

                                }


                                {

                                }


                                {

                                }


                                {
                                    {
                                        {
                                            {
                                                {
                                                }
                                            }
                                        }

                                    }


                                    {
                                        {
                                            {
                                                {
                                                    {
                                                                     
                                                    }
                                                }
                                                                                            }

                                            }


                                            {
                                                {
                                                    {
                                                                                                  
                                                        {
                                                            {
                                                                                               
                                                        


                                                        
                                                            }
                                                