using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct Chunk
{
    public Vector3 chunk_pos;
    public int chunk_ID;
    public string directory;
    public List<Obj> objects;
    public Bounds chunk_bounds;
    public BoxCollider chunk_collider;
    public List<int> chunk_neighbours;
}
