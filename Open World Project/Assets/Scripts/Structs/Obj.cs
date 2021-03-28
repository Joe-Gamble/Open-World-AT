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

[Serializable]
public class Obj
{
    public ObjectTypes obj_type = ObjectTypes.BASIC;
    public GameObject runtime_ref;

    public string obj_parent = null;
    public List<string> child_names = null;

    public string name;

    public Vector3 obj_position;
    public Quaternion obj_rotation;
    public Vector3 obj_scale;

    public bool is_unity_primitive = false;
    public PrimitiveType primitive_type;

    public string obj_mesh;
    public List<string> obj_mats;
    public string collider_type;

    public void SaveObjectData(GameObject go)
    {
        obj_mats = new List<string>();

        if (go.TryGetComponent(out MeshRenderer mr))
        {
            Material[] mats = mr.sharedMaterials;
            Mesh sm = go.GetComponent<MeshFilter>().sharedMesh;

            foreach (Material mat in mats)
            {
                obj_mats.Add(mat.name);
            }

            obj_mesh = sm.name;

            string m_path = "Assets/Resources/World Data/";

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

            //Materials
            if (obj_mats.Count > 0)
            {
                foreach (Material mat in mats)
                {
                    //Debug.Log("Moving Asset");
                    if (Resources.Load("World Data/Materials/" + mat) == null)
                    {
                        string status = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(mat), m_path + "Materials/" + mat.name + ".mat");
                        //Debug.Log(status);
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

    public void Initialse(GameObject go)
    {
        if (go.transform.parent != null)
        {
            if (obj_parent == null)
            {
                obj_parent = go.transform.parent.name;
            }
        }

        if (go.transform.childCount > 0)
        {
            child_names = new List<string>();

            Transform[] children = go.GetComponentsInChildren<Transform>();

            foreach (Transform child in children)
            {
                child_names.Add(child.name);
            }
        }

        SaveObjectData(go);

        Transform trans = go.GetComponent<Transform>();

        name = trans.name;

        obj_position = trans.position;
        obj_rotation = trans.rotation;
        obj_scale = trans.localScale;

        runtime_ref = go;
    }

    public void SpawnObject()
    {
        GameObject go;
        List<Material> mats = new List<Material>();

        //Unity Primitive
        if (is_unity_primitive)
        {
            go = GameObject.CreatePrimitive(primitive_type);

            Material default_mat = go.GetComponent<MeshRenderer>().sharedMaterial;

            if (obj_mats != null)
            {
                foreach (string mat_name in obj_mats)
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

            if (obj_mesh != "")
            {
                go.AddComponent<MeshFilter>().sharedMesh = Resources.Load<Mesh>("World Data/Meshes/" + obj_mesh);
                go.AddComponent<MeshRenderer>();
            }

            if (obj_mats != null)
            {
                foreach (string mat_name in obj_mats)
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
    public Obj basic_object;
    public Basic(GameObject go)
    {
        basic_object = new Obj();
        basic_object.Initialse(go);
    }
}

[Serializable]
public class Entity
{
    public Obj entity_object;

    public NavMeshAgent ent_agent;
    public int health;

    public Entity(GameObject go)
    {
        entity_object = new Obj();
        entity_object.Initialse(go);
    }
    /*
    public Entity(Obj obj)
    {
        obj_type = ObjectTypes.ENTITY;
        runtime_ref = obj.runtime_ref;

        obj_parent = obj.obj_parent;
        child_names = obj.child_names;

        name = obj.name;

        obj_position = obj.obj_position;
        obj_rotation = obj.obj_rotation;
        obj_scale = obj.obj_scale;

        is_unity_primitive = obj.is_unity_primitive;
        primitive_type = obj.primitive_type;

        obj_mesh = obj.obj_mesh;
        obj_mats = obj.obj_mats;
        collider_type = obj.collider_type;
    }
    */
}
