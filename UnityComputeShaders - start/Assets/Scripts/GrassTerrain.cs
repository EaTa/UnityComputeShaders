using UnityEngine;

public class GrassTerrain : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public ComputeShader shader;

    [Range(0, 3)] public float density = 0.8f;

    [Range(0.1f, 3)] public float scale = 0.2f;

    [Range(10, 45)] public float maxLean = 25;

    [Range(0, 1)] public float heightAffect = 0.5f;

    readonly uint[] argsArray = { 0, 0, 0, 0, 0 };
    ComputeBuffer argsBuffer;
    Bounds bounds;
    GrassClump[] clumpsArray;

    ComputeBuffer clumpsBuffer;
    int groupSize;
    int kernelLeanGrass;
    readonly int SIZE_GRASS_CLUMP = 5 * sizeof(float);
    int timeID;

    // Start is called before the first frame update
    void Start()
    {
        bounds = new Bounds(Vector3.zero, new Vector3(30, 30, 30));
        InitShader();
    }

    // Update is called once per frame
    void Update()
    {
        shader.SetFloat(timeID, Time.time);
        shader.Dispatch(kernelLeanGrass, groupSize, 1, 1);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    void OnDestroy()
    {
        clumpsBuffer.Release();
        argsBuffer.Release();
    }

    void InitShader()
    {
        var mf = GetComponent<MeshFilter>();
        var bounds = mf.sharedMesh.bounds;

        var clumps = bounds.extents;
        var vec = transform.localScale / 0.1f * density;
        clumps.x *= vec.x;
        clumps.z *= vec.z;

        var total = (int)clumps.x * (int)clumps.z;

        kernelLeanGrass = shader.FindKernel("LeanGrass");

        uint threadGroupSize;
        shader.GetKernelThreadGroupSizes(kernelLeanGrass, out threadGroupSize, out _, out _);
        groupSize = Mathf.CeilToInt(total / (float)threadGroupSize);
        var count = groupSize * (int)threadGroupSize;

        InitPositionsArray(count, bounds);

        count = clumpsArray.Length;

        clumpsBuffer = new ComputeBuffer(count, SIZE_GRASS_CLUMP);
        clumpsBuffer.SetData(clumpsArray);

        shader.SetBuffer(kernelLeanGrass, "clumpsBuffer", clumpsBuffer);
        shader.SetFloat("maxLean", maxLean * Mathf.PI / 180);
        timeID = Shader.PropertyToID("time");

        argsArray[0] = mesh.GetIndexCount(0);
        argsArray[1] = (uint)count;
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        material.SetBuffer("clumpsBuffer", clumpsBuffer);
        material.SetFloat("_Scale", scale);
    }

    void InitPositionsArray(int count, Bounds bounds)
    {
        clumpsArray = new GrassClump[count];
    }

    struct GrassClump
    {
        public Vector3 position;
        public float lean;
        public readonly float noise;

        public GrassClump(Vector3 pos)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            lean = 0;
            noise = Random.Range(0.5f, 1);
            if (Random.value < 0.5f) noise = -noise;
        }
    }
}