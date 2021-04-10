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
    public Matrix4x4[] bindPoses;
}

[Serializable]
public class Entity
{
    public string name;
    public TransformData transform_data;

    public GameObject entity_ref;

    public SkinnedMeshData mesh_data;
    public List<TransformData> objects;

    public bool ent_agent;
    public string ent_animator = "";
    public int health;

    public int index = 2;

    public Entity(GameObject go)
    {
        name = go.name;
        objects = new List<TransformData>();
        mesh_data = new SkinnedMeshData();

        transform_data = InitChildren(new TransformData(go.transform.parent.gameObject), go.transform);

        SaveEntityData(go);
    }

    public void SaveEntityData(GameObject go)
    {
        if (go.TryGetComponent(out Animator ani))
        {
            string r_path = "World Data/Entities/Animators/" + ani.name;
            if (Resources.Load("World Data/Entities/Animators/" + ani.name) != null)
            {
                ent_animator = r_path;
            }
        }
        if (go.TryGetComponent(out NavMeshAgent agent))
        {
            ent_agent = true;
        }
        AssetDatabase.Refresh();
    }

    public void LoadEntityData()
    {
        GameObject entity_go = transform_data.runtime_ref;
        if (entity_go != null)
        {
            if (ent_animator != "")
            {
                Animator ani = entity_go.AddComponent<Animator>();
                ani.runtimeAnimatorController = Resources.Load(ent_animator) as RuntimeAnimatorController;
            }
            if (ent_agent)
            {
                NavMeshAgent agent = entity_go.AddComponent<NavMeshAgent>();
                agent.speed = 1;

                entity_go.AddComponent<EntityMovement>();
            }
        }
    }

    public TransformData InitChildren(TransformData parent_data, Transform obj)
    {
        TransformData obj_data = new TransformData(obj.gameObject);
        obj_data.id = index;
        index += 1;

        if (obj_data.parent == -1)
        {
            obj_data.parent = parent_data.id;
        }

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

                    //BindPoses
                    mesh_data.bindPoses = smr.sharedMesh.bindposes;
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
                    return new TransformData(go);
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
        foreach (TransformData obj_data in entity.objects)
        {
            Transform obj_trans = obj_data.runtime_ref.transform;

            if (obj_data.parent == (int)ObjManager.StaticObjects.WORLD_ENTITIES)
            {
                obj_trans.parent = GameObject.Find("World Entities").transform;
            }
            else if (obj_data.parent == (int)ObjManager.StaticObjects.WORLD_OBJECTS)
            {
                obj_trans.parent = GameObject.Find("World Objects").transform;
            }
            else
            {
                TransformData parent_trans = entity.FindObject(obj_data.parent);

                if (parent_trans != null)
                {
                    Transform parent;

                    parent = parent_trans.runtime_ref.transform;
                    obj_trans.parent = parent;
                }
            }
        }
    }

    public static void SpawnEntity(TransformData ent)
    {
        foreach (Entity entity in GetCollection().entities)
        {
            if (entity.name == ent.name)
            {
                foreach (TransformData data in entity.objects)
                {
                    GameObject go = new GameObject();

                    go.name = data.name;

                    go.transform.position = data.position;
                    go.transform.rotation = data.rotation;
                    go.transform.localScale = data.scale;

                    data.runtime_ref = go;

                    if (data.id == entity.transform_data.id)
                    {
                        entity.transform_data = data;
                        ent.runtime_ref = go;
                    }
                }

                entity.mesh_data.mesh_object.Spawn();

                LinkEntity(entity);

                SkinnedMeshData mesh_data = entity.mesh_data;
                Basic entity_obj = mesh_data.mesh_object;
                GameObject mesh_object = entity_obj.transform_data.runtime_ref;

                Transform root = entity.FindObject(mesh_data.root_ref).runtime_ref.transform;
                Transform armature = entity.transform_data.runtime_ref.transform.Find("Armature");

                //root.localScale = new Vector3(1, 1, 1);
                List<Transform> bone_data = new List<Transform>();

                foreach (int bone_id in mesh_data.bones_refs)
                {
                    bone_data.Add(entity.FindObject(bone_id).runtime_ref.transform);
                }

                Transform[] bones = bone_data.ToArray();

                Matrix4x4[] bindPoses = mesh_data.bindPoses;

                SkinnedMeshRenderer smr = mesh_object.AddComponent<SkinnedMeshRenderer>();

                smr.rootBone = root;

                List<Material> mats = new List<Material>();

                Mesh mesh = Resources.Load<Mesh>("World Data/Meshes/" + entity_obj.obj_mesh);

                mesh.boneWeights = mesh_data.bone_weights.ToArray();

                mesh.bindposes = bindPoses;
                smr.bones = bones;
                smr.sharedMesh = mesh;

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

                entity.LoadEntityData();

                foreach (TransformData data in entity.objects)
                {
                    if (data.id == entity.mesh_data.mesh_object.transform_data.id)
                    {
                        GameObject mesh_go = entity.mesh_data.mesh_object.transform_data.runtime_ref;

                        mesh_go.transform.parent = data.runtime_ref.transform.parent;
                        WorldManager.DeleteObject(data);
                    }
                }

                if (entity.transform_data.runtime_ref != null)
                {
                    entity.transform_data.runtime_ref.transform.position = ent.position;
                    entity.transform_data.runtime_ref.transform.rotation = ent.rotation;
                    entity.transform_data.runtime_ref.transform.localScale = ent.scale;
                }
            }
        }
    }

    public static void Despawn(TransformData data)
    {
        WorldManager.DeleteObject(data);
    }
}
