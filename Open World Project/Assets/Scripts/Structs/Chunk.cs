using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct Chunk
{
    public Vector3 chunk_pos;
    public int chunk_ID;
    public List<Obj> objects;
    public Bounds chunk_bounds;
    public BoxCollider chunk_collider;
    public List<int> chunk_neighbours;
}

[Serializable]
public struct ChunkData
{
    public Chunk current_chunk;
    public float chunk_size;

    public string directory;
    public List<Chunk> chunks;
}
