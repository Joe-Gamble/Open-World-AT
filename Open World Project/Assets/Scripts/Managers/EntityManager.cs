using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.IO;
using UnityEngine.AI;

[Serializable]
public struct SkinnedMeshData
{
    public string root_name;
    public List<int> bones;
    public string mesh_object;
}

[Serializable]
public class Entity
{
    public SkinnedMeshData mesh_data;

    public Basic entity_object;

    public NavMeshAgent ent_agent;
    public Animator ent_animator;
    public int health;

    public Entity(GameObject go)
    {
        entity_object = new Basic(ChunkManager.GetChunkAtLoc(go.transform.position), go);
        mesh_data = new SkinnedMeshData();

        //test
    }

    public Obj[] ExtractChildren(Transform[] bones)
    {
        List<Obj> children = new List<Obj>();
        foreach (Transform bone in bones)
        {
            /*
            Obj obj = FindObject(bone.name);
            if (obj == null)
            {
                Debug.Log("null");
            }
            children.Add(obj);
            */
        }
        return children.ToArray();
    }

    public void InitialiseEntity()
    {
        Transform entity = entity_object.obj_data.runtime_ref.transform;

        Transform root_obj = null;
        Transform mesh_object = null;
        Obj[] bones = null;

        for (int i = 0; i < entity.childCount; i++)
        {
            Transform child = entity.GetChild(i).transform;

            if (child.gameObject.TryGetComponent(out SkinnedMeshRenderer smr))
            {
                foreach (Transform bone in smr.bones)
                {
                    Debug.Log(bone.name);
                }
                bones = ExtractChildren(smr.bones);
                mesh_object = child;
                root_obj = smr.rootBone;
            }
        }

        if (root_obj != null && mesh_object != null && bones != null)
        {
            SaveMeshData(root_obj, bones, mesh_object);
        }

    }

    public void SaveMeshData(Transform _root_object, Obj[] _bones, Transform _mesh_object)
    {
        mesh_data.bones = new List<int>();

        mesh_data.root_name = _root_object.name;

        foreach (Obj bone in _bones)
        {
            Debug.Log(_bones.Length);
            Debug.Log(bone.obj_id);
            //entity_data.bones.Add(bone.obj_id);
        }

        mesh_data.mesh_object = _mesh_object.name;
    }

    /*

private void SaveEntityData(GameObject go)
{
    if (go.TryGetComponent(out NavMeshAgent agent))
    {
        ent_agent = agent;
    }

    if (go.TryGetComponent(out Animator animator))
    {
        ent_animator = animator;
    }
}

*/
}

public struct EntityCollection
{
    public int num_entities;
    public List<Entity> entities;
}

public static class EntityManager
{
    public static EntityCollection collection;
    static string path = Application.dataPath + "/Resources/World Data/Entities/EntityData.json";

    public static List<Entity> GetEntityList()
    {
        List<Entity> entity_list = new List<Entity>();
        collection = JsonUtility.FromJson<EntityCollection>(path);

        entity_list = collection.entities;
        return entity_list;
    }

    public static string AddEntity(GameObject go)
    {
        if (collection.entities == null)
        {
            collection.entities = new List<Entity>();
            collection.num_entities = 0;
        }

        foreach (Entity ent in collection.entities)
        {
            if (go.name == ent.entity_object.obj_data.name)
            {
                return ent.entity_object.obj_data.name;
            }
        }

        Entity new_entity = new Entity(go);
        collection.entities.Add(new_entity);
        collection.num_entities += 1;

        return new_entity.entity_object.obj_data.name;
    }

    public static void OverrideEntityData()
    {
        string json = JsonUtility.ToJson(collection);
        File.WriteAllText(path, json);
    }

    public static void SpawnEntity(string name)
    {
        foreach (Entity entity in GetEntityList())
        {
            if (entity.entity_object.obj_data.name == name)
            {
                /*
                SkinnedMeshData entity_data = entity.mesh_data;
                Obj entity_obj = FindObject(entity_data.mesh_object);

                Debug.Log(entity_obj.name);

                GameObject mesh_object = entity_obj.runtime_ref;
                Transform root = FindObject(entity_data.root_name).runtime_ref.transform;
                List<Transform> bone_data = new List<Transform>();

                foreach (int bone_id in entity_data.bones)
                {
                    bone_data.Add(FindObject(bone_id).runtime_ref.transform);
                }

                Transform[] bones = bone_data.ToArray();

                SkinnedMeshRenderer smr = mesh_object.AddComponent<SkinnedMeshRenderer>();
                smr.bones = bones;
                smr.rootBone = root;
                smr.sharedMesh = Resources.Load<Mesh>("World Data/Meshes/" + entity_obj.obj_mesh);

                List<Material> mats = new List<Material>();

                if (entity_obj.obj_mats != null)
                {
                    foreach (string mat_name in entity_obj.obj_mats)
                    {
                        if (mat_name != "Default-Material")
                        {
                            Material m = Resources.Load("World Data/Materials/" + mat_name, typeof(Material)) as Material;
                            mats.Add(m);
                        }
                    }
                    smr.sharedMaterials = mats.ToArray();
                }
                */
            }
        }
    }
}
