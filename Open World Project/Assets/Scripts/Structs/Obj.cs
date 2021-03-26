using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
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
}

[Serializable]
public class Entity : Obj
{
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
    public NavMeshAgent ent_agent;
    public int health;
}
