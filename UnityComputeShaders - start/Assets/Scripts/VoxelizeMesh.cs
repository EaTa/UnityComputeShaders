using System.Collections.Generic;
using UnityEngine;

public class VoxelizeMesh : MonoBehaviour
{
    public Mesh meshToVoxelize;
    public int yParticleCount = 4;
    public int layer = 9;

    public float ParticleSize { get; } = 0;

    public List<Vector3> PositionList { get; } = new List<Vector3>();

    public void Voxelize(Mesh mesh)
    {
    }
}