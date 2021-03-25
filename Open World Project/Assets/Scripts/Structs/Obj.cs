using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

enum ObjectTypes
{
    BASIC,
    ENTITY
}

[Serializable]
public class Obj
{
    ObjectTypes object_type = ObjectTypes.BASIC;
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

}
