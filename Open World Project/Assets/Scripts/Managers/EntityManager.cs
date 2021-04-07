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
    public Basic mesh_object;
    public int root_ref;
    public int[] bones_refs;
    public List<BoneWeight> bone_weights;
}

[Serializable]
public class Entity
{
    public string name;
    public TransformData transform_data;

    public GameObject entity_ref;

    public SkinnedMeshData mesh_data;
    public List<TransformData> objects;

    public NavMeshAgent ent_agent;
    public Animator ent_animator;
    public int health;

    public int index = 3;

    public Entity(GameObject go)
    {
        name = go.name;
        objects = new List<TransformData>();
        mesh_data = new SkinnedMeshData();

        transform_data = new TransformData(go);
        transform_data.id = index;

        transform_data = InitChildren(transform_data, go.transform);
    }

    public TransformData InitChildren(TransformData parent_data, Transform obj)
    {
        TransformData obj_data = new TransformData(obj.gameObject);
        obj_data.id = index += 1;
        obj_data.parent = parent_data.id;
        obj_data.runtime_ref = obj.gameObject;

        if (obj.childCount > 0)
        {
            obj_data.children = new List<int>();
            foreach (Transform child in obj)
            {
                if (child.TryGetComponent(out SkinnedMeshRenderer smr))
                {
                    //SkinnedMeshData serialization

                    //Mesh Object
                    GameObject mesh_go = child.gameObject;
                    mesh_data.mesh_object = new Basic(ChunkManager.GetChunkAtLoc(mesh_go.transform.position), mesh_go, false);

                    mesh_data.mesh_object.transform_data = InitChildren(obj_data, child);
                    obj_data.children.Add(mesh_data.mesh_object.transform_data.id);

                    //Root Object
                    mesh_data.root_ref = FindObject(smr.rootBone.name).id;

                    mesh_data.bone_weights = new List<BoneWeight>();

                    foreach (BoneWeight weight in smr.sharedMesh.boneWeights)
                    {
                        mesh_data.bone_weights.Add(weight);
                    }

                    //Bones Array
                    mesh_data.bones_refs = GetBones(smr.bones);
                }
                else if (child != obj)
                {
                    obj_data.children.Add(InitChildren(obj_data, child).id);
                }
            }
        }
        objects.Add(obj_data);
        return obj_data;
    }

    public TransformData FindObject(int id)
    {
        if (id != (int)ObjManager.StaticObjects.WORLD_ENTITIES && id != (int)ObjManager.StaticObjects.WORLD_OBJECTS)
        {
            foreach (TransformData obj in objects)
            {
                if (obj.id == id)
                {
                    return obj;
                }
            }
        }

        return null;
    }

    public TransformData FindObject(string name)
    {
        foreach (TransformData obj in objects)
        {
            if (obj.name == name)
            {
                return obj;
            }
        }
        return null;
    }

    public int[] GetBones(Transform[] bones)
    {
        List<int> bone_refs = new List<int>();

        foreach (Transform bone in bones)
        {
            TransformData bone_data = FindObject(bone.name);

            if (bone_data != null)
            {
                bone_refs.Add(bone_data.id);
            }
        }
        return bone_refs.ToArray();
    }

    public TransformData[] ExtractChildren(Transform[] bones)
    {
        List<TransformData> children = new List<TransformData>();

        foreach (Transform bone in bones)
        {

        }
        return children.ToArray();
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

[Serializable]
public struct EntityCollection
{
    public int num_entities;
    public List<Entity> entities;
}

public class EntityManager
{
    public static EntityCollection collection;
    static string path = Application.dataPath + "/Resources/World Data/Entities/EntityData.json";
    //"

    public static EntityCollection GetCollection()
    {
        string content = File.ReadAllText(path);

        EntityCollection collection = new EntityCollection();
        collection = JsonUtility.FromJson<EntityCollection>(content);

        return collection;
    }

    public static void RefreshList()
    {
        collection = new EntityCollection();
        collection = GetCollection();
        AssetDatabase.Refresh();
    }

    public static TransformData AddEntity(Chunk chunk, GameObject go)
    {
        if (collection.entities == null)
        {
            collection.entities = new List<Entity>();
            collection.num_entities = 0;
        }
        else
        {
            foreach (Entity ent in collection.entities)
            {
                if (go.name == ent.name)
                {
                    return ent.transform_data;
                }
            }
        }
        Entity new_entity = new Entity(go);
        collection.entities.Add(new_entity);
        collection.num_entities += 1;

        return new_entity.transform_data;
    }

    public static void OverrideEntityData()
    {
        string json = JsonUtility.ToJson(collection);
        File.WriteAllText(path, json);
    }

    public static void LinkEntity(Entity entity)
    {
        Debug.Log("yes");
        foreach (TransformData obj_data in entity.objects)
        {
            Transform obj_trans = obj_data.runtime_ref.transform;
            TransformData parent_trans = entity.FindObject(obj_data.parent);

            Transform parent;

            if (parent_trans != null)
            {
                if (parent_trans.id == (int)ObjManager.StaticObjects.WORLD_ENTITIES)
                {
                    Debug.Log("Origin");
                    parent = GameObject.Find("World Entities").transform;
                }
                else if (parent_trans.id == (int)ObjManager.StaticObjects.WORLD_ENTITIES)
                {
                    parent = GameObject.Find("World Objects").transform;
                }
                else
                {
                    parent = parent_trans.runtime_ref.transform;
                }
                obj_trans.parent = parent;
            }
        }
    }

    public static void SpawnEntity(TransformData ent)
    {
        foreach (Entity entity in GetCollection().entities)
        {
            if (entity.name == ent.name)
            {
                entity.mesh_data.mesh_object.Spawn();
                foreach (TransformData data in entity.objects)
                {
                    GameObject go = new GameObject();

                    go.name = data.name;

                    go.transform.position = data.position;
                    go.transform.rotation = data.rotation;
                    go.transform.localScale = data.scale;

                    data.runtime_ref = go;
                }

                LinkEntity(entity);

                foreach (TransformData data in entity.objects)
                {
                    if (data.id == entity.mesh_data.mesh_object.transform_data.id)
                    {
                        GameObject mesh = entity.mesh_data.mesh_object.transform_data.runtime_ref;

                        mesh.transform.parent = data.runtime_ref.transform.parent;
                        WorldManager.DeleteObject(data);
                    }

                    if (data.id == entity.transform_data.id)
                    {
                        data.runtime_ref.transform.position = ent.position;
                        data.runtime_ref.transform.rotation = ent.rotation;
                        data.runtime_ref.transform.localScale = ent.scale;

                        ent.runtime_ref = data.runtime_ref;
                    }
                }


                SkinnedMeshData mesh_data = entity.mesh_data;
                Basic entity_obj = mesh_data.mesh_object;
                GameObject mesh_object = entity_obj.transform_data.runtime_ref;

                Transform root = entity.FindObject(mesh_data.root_ref).runtime_ref.transform;
                root.localScale = new Vector3(1, 1, 1);
                List<Transform> bone_data = new List<Transform>();

                foreach (int bone_id in mesh_data.bones_refs)
                {
                    bone_data.Add(entity.FindObject(bone_id).runtime_ref.transform);
                }

                Transform[] bones = bone_data.ToArray();

                List<Matrix4x4> bindPoses = new List<Matrix4x4>();

                foreach (Transform bone in bones)
                {
                    Matrix4x4 new_pose = bone.worldToLocalMatrix * mesh_object.transform.localToWorldMatrix;
                    bindPoses.Add(new_pose);
                }

                SkinnedMeshRenderer smr = mesh_object.AddComponent<SkinnedMeshRenderer>();

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

                smr.sharedMesh = Resources.Load<Mesh>("World Data/Meshes/" + entity_obj.obj_mesh);

                smr.sharedMesh.bindposes = bindPoses.ToArray();

                smr.sharedMesh.boneWeights = mesh_data.bone_weights.ToArray();

                smr.rootBone = root;
                smr.bones = bones;
            }
        }
    }

    public static void Despawn(TransformData data)
    {
        Debug.Log(data.name);
        WorldManager.DeleteObject(data);
    }
}
