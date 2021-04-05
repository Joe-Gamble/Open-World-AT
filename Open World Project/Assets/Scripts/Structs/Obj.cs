using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using UnityEngine.AI;

public enum ObjectTypes
{
    BASIC,
    ENTITY
}

public static class ObjManager
{
    public enum StaticObjects
    {
        WORLD_OBJECTS = 0,
        WORLD_ENTITIES = 1
    }

    static int obj_index = 2;

    public static List<Basic> objs = new List<Basic>();

    public static void AddObject(Basic obj, bool is_new)
    {
        if (is_new)
        {
            obj.obj_data.obj_id = obj_index;
            obj_index += 1;
        }
        objs.Add(obj);
    }

    public static Basic FindObject(string name)
    {
        foreach (Basic obj in objs)
        {
            if (obj.obj_data.name == name)
            {
                return obj;
            }
        }
        return null;
    }

    public static Basic FindObject(int id)
    {
        foreach (Basic obj in objs)
        {
            if (obj.obj_data.obj_id == id)
            {
                return obj;
            }
        }
        return null;
    }
}

[Serializable]
public class Obj
{
    public string name;
    public int obj_id = -1;

    public ObjectTypes obj_type = ObjectTypes.BASIC;
    public GameObject runtime_ref;

    public Vector3 obj_position;
    public Quaternion obj_rotation;
    public Vector3 obj_scale;

    public bool is_unity_primitive = false;
    public PrimitiveType primitive_type;

    public string obj_mesh;
    public string renderer_type;
    public List<string> obj_mats;
    public string collider_type;

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

    private void SaveMats(string m_path, Material[] mats)
    {
        if (mats.Length > 0)
        {
            foreach (Material mat in mats)
            {
                obj_mats.Add(mat.name);

                if (Resources.Load("World Data/Materials/" + mat) == null)
                {
                    string status = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(mat), m_path + "Materials/" + mat.name + ".mat");
                    //Debug.Log(status);
                }
            }
        }
    }

    public void Initialise(Basic obj, GameObject go)
    {
        ObjManager.AddObject(obj, true);

        SaveObjectData(go);

        Transform trans = go.GetComponent<Transform>();

        name = go.name;

        obj_position = trans.position;
        obj_rotation = trans.rotation;
        obj_scale = trans.localScale;

        runtime_ref = go;
    }

    public void SpawnObject()
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
                else if (renderer_type == "UnityEngine.SkinnedMeshRenderer")
                {
                    //chunk.skinned_mesh_objects.Add(this);
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

        go.GetComponent<Transform>().position = obj_position;
        go.GetComponent<Transform>().rotation = obj_rotation;
        go.GetComponent<Transform>().localScale = obj_scale;

        go.name = name;
        runtime_ref = go;
    }
}


[Serializable]
public class Basic
{
    public Obj obj_data;

    public int obj_parent = -1;
    public List<int> obj_children = null;

    public Basic(Chunk chunk, GameObject go)
    {
        obj_data = new Obj();
        obj_data.Initialise(this, go);

        if (go.transform.childCount > 0)
        {
            Transform[] children = go.GetComponentsInChildren<Transform>();

            foreach (Transform child in children)
            {
                if (child != go.transform)
                {
                    Basic obj = new Basic(ChunkManager.GetChunkAtLoc(child.transform.position), child.gameObject);
                }
            }
        }

        chunk.basic_objects.Add(this);
    }
}
