using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Obj
{
    public GameObject runtime_ref;

    public string obj_parent = null;
    public List<string> child_names = null;

    public string name;

    public Vector3 obj_position;
    public Quaternion obj_rotation;
    public Vector3 obj_scale;

    public bool is_unity_primitive = false;
    public PrimitiveType primitive_type;

    public Mesh obj_mesh;
    public Material[] obj_mats;
    public Collider obj_col;
}
