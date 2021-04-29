using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum ObjectTypes
{
    BASIC,
    ENTITY
}

[Serializable]
public class TransformData
{
    public TransformData(GameObject go)
    {
        Transform trans = go.GetComponent<Transform>();
        name = go.name;

        parent = -1;
        children = null;

        runtime_ref = go;

        position = trans.position;
        rotation = trans.rotation;
        scale = trans.localScale;

        if (go.transform.parent != null)
        {
            if (go.transform.parent.name == "World Objects")
            {
                AddParent((int)ObjManager.StaticObjects.WORLD_OBJECTS);
            }
            else if (go.transform.parent.name == "World Entities")
            {
                AddParent((int)ObjManager.StaticObjects.WORLD_ENTITIES);
            }
        }
    }

    public void AddParent(int parent_id)
    {
        parent = parent_id;
    }

    public void InitID(int new_id)
    {
        id = new_id;
    }

    public string name;
    public int id;

    public GameObject runtime_ref;

    public int parent;
    public List<int> children;

    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

public static class ObjManager
{
    public enum StaticObjects
    {
        WORLD_OBJECTS = 0,
        WORLD_ENTITIES = 1
    }

    //get rid of all functioanlity besides increment 

    static int index = 2;

    public static int GetUniqueNumber()
    {
        index += 1;
        return index;
    }

    public static void LoadObject(ChunkData chunk_data, Basic obj)
    {
        chunk_data.pending_childs.Add(obj);
        chunk_data.world_objects.Add(obj);
    }

    public static Basic FindObject(List<Basic> objects, int obj_id)
    {
        foreach (Basic obj in objects)
        {
            if (obj.transform_data.id == obj_id)
            {
                return obj;
            }
        }
        return null;
    }

    public static void RemoveObject(ChunkData chunk_data, Basic obj)
    {
        chunk_data.pending_childs.Remove(obj);
        chunk_data.world_objects.Remove(obj);
    }
}

[Serializable]
public class Basic
{
    public Basic(Chunk chunk, GameObject go, bool recursive)
    {
        Initialise(go);
        transform_data = new TransformData(go);

        transform_data.InitID(ObjManager.GetUniqueNumber());

        if (recursive)
        {
            if (go.transform.childCount > 0)
            {
                transform_data.children = new List<int>();

                Transform[] children_objects = go.GetComponentsInChildren<Transform>();

                foreach (Transform child in children_objects)
                {
                    if (child != go.transform)
                    {
                        Basic new_obj = new Basic(ChunkManager.GetChunkAtLoc(child.transform.position), child.gameObject, true);
                        new_obj.transform_data.AddParent(this.transform_data.id);
                        transform_data.children.Add(new_obj.transform_data.id);
                    }
                }
            }
            chunk.basic_objects.Add(this);
        }
    }


    public TransformData transform_data;

    public ObjectTypes obj_type = ObjectTypes.BASIC;

    public bool is_unity_primitive = false;
    public PrimitiveType primitive_type;

    public string obj_mesh;
    public string renderer_type;
    public List<string> obj_mats;
    public string collider_type;

#if UNITY_EDITOR

    public void SaveObjectData(GameObject go)
    {
        if (go.TryGetComponent(out Renderer r))
        {
            string m_path = "Assets/Resources/World Data/";

            obj_mats = new List<string>();

            Mesh sm = new Mesh();

            renderer_type = r.GetType().ToString();

            if (renderer_type == "UnityEngine.MeshRenderer")
            {
                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                sm = go.GetComponent<MeshFilter>().sharedMesh;
                obj_mesh = sm.name;
                SaveMats(m_path, mr.sharedMaterials);
            }
            else if (renderer_type == "UnityEngine.SkinnedMeshRenderer")
            {
                SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
                sm = smr.sharedMesh;
                obj_mesh = sm.name;
                SaveMats(m_path, smr.sharedMaterials);
            }

            //Meshes
            if (obj_mesh != null)
            {
                if (Resources.Load("World Data/Meshes/" + obj_mesh) == null)
                {
                    string status = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(sm), m_path + "Meshes/" + obj_mesh + ".fbx");
                    if (status != "")
                    {
                        // print(status);
                        is_unity_primitive = true;

                        switch (obj_mesh)
                        {
                            case "Cube":
                                {
                                    primitive_type = PrimitiveType.Cube;
                                    break;
                                }
                            case "Sphere":
                                {
                                    primitive_type = PrimitiveType.Sphere;
                                    break;
                                }
                            case "Capsule":
                                {
                                    primitive_type = PrimitiveType.Capsule;
                                    break;
                                }
                            case "Plane":
                                {
                                    primitive_type = PrimitiveType.Plane;
                                    break;
                                }
                            case "Cylinder":
                                {
                                    primitive_type = PrimitiveType.Cylinder;
                                    break;
                                }
                            case "Quad":
                                {
                                    primitive_type = PrimitiveType.Quad;
                                    break;
                                }
                            default:
                                {
                                    primitive_type = PrimitiveType.Cube;
                                    break;
                                }
                        }
                    }
                }
            }
            if (go.TryGetComponent(out Collider col))
            {
                collider_type = col.GetType().ToString();
            }

            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
    }
#endif


    private void SaveMats(string m_path, Material[] mats)
    {
        if (mats.Length > 0)
        {
            foreach (Material mat in mats)
            {
                obj_mats.Add(mat.name);

                if (Resources.Load("World Data/Materials/" + mat) == null)
                {
#if UNITY_EDITOR
                    string status = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(mat), m_path + "Materials/" + mat.name + ".mat");
#endif
                    //Debug.Log(status);
                }
            }
        }
    }


    public void Initialise(GameObject go)
    {
#if UNITY_EDITOR
        SaveObjectData(go);
#endif
    }

    public void Spawn()
    {
        GameObject go;
        List<Material> mats = new List<Material>();

        if (obj_mats != null)
        {
            foreach (string mat_name in obj_mats)
            {
                if (mat_name != "Default-Material")
                {
                    Material m = Resources.Load("World Data/Materials/" + mat_name, typeof(Material)) as Material;
                    mats.Add(m);
                }
            }
        }

        //Unity Primitive
        if (is_unity_primitive)
        {
            go = GameObject.CreatePrimitive(primitive_type);

            MeshRenderer mr = go.GetComponent<MeshRenderer>();

            Material default_mat = mr.sharedMaterial;

            if (obj_mats.Count > 0)
            {
                foreach (string mat_name in obj_mats)
                {
                    if (mat_name == "Default-Material")
                    {
                        mats.Add(default_mat);
                    }
                }
                mr.sharedMaterials = mats.ToArray();
            }
        }

        //Non Unity Object
        else
        {
            go = new GameObject();

            if (obj_mesh != "")
            {
                if (renderer_type == "UnityEngine.MeshRenderer")
                {
                    MeshRenderer mr = go.AddComponent<MeshRenderer>();
                    go.AddComponent<MeshFilter>().sharedMesh = Resources.Load<Mesh>("World Data/Meshes/" + obj_mesh);
                    mr.sharedMaterials = mats.ToArray();
                }
            }
        }

        if (collider_type != "")
        {
            switch (collider_type)
            {
                case "UnityEngine.MeshCollider":
                    {
                        go.AddComponent<MeshCollider>();
                        break;
                    }
                case "UnityEngine.BoxCollider":
                    {
                        go.AddComponent<BoxCollider>();
                        break;
                    }
                case "UnityEngine.SphereCollider":
                    {
                        go.AddComponent<SphereCollider>();
                        break;
                    }
                case "UnityEngine.CapsuleCollider":
                    {
                        go.AddComponent<CapsuleCollider>();
                        break;
                    }
            }
        }

        go.GetComponent<Transform>().position = transform_data.position;
        go.GetComponent<Transform>().rotation = transform_data.rotation;
        go.GetComponent<Transform>().localScale = transform_data.scale;

        go.name = transform_data.name;
        transform_data.runtime_ref = go;
    }
}

