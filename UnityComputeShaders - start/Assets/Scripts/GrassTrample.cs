using UnityEngine;

public class GrassTrample : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public ComputeShader shader;

    [Range(0, 1)] public float density;

    [Range(0.1f, 3)] public float scale;

    [Range(0.5f, 3)] public float speed;

    [Range(10, 45)] public float maxLean;

    public Transform trampler;

    [Range(0.1f, 2)] public float trampleRadius = 0.5f;

    readonly uint[] argsArray = { 0, 0, 0, 0, 0 };
    ComputeBuffer argsBuffer;
    Bounds bounds;
    GrassClump[] clumpsArray;

    ComputeBuffer clumpsBuffer;
    int groupSize;
    int kernelUpdateGrass;
    Vector4 pos;
    readonly int SIZE_GRASS_CLUMP = 10 * sizeof(float);
    int timeID;
    int tramplePosID;

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
        pos = trampler.position;
        shader.SetVector(tramplePosID, pos);

        shader.Dispatch(kernelUpdateGrass, groupSize, 1, 1);

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
        var size = new Vector2(bounds.extents.x * transform.localScale.x, bounds.extents.z * transform.localScale.z);

        var clumps = size;
        var vec = transform.localScale / 0.1f * density;
        clumps.x *= vec.x;
        clumps.y *= vec.z;

        var total = (int)clumps.x * (int)clumps.y;

        kernelUpdateGrass = shader.FindKernel("UpdateGrass");

        uint threadGroupSize;
        shader.GetKernelThreadGroupSizes(kernelUpdateGrass, out threadGroupSize, out _, out _);
        groupSize = Mathf.CeilToInt(total / (float)threadGroupSize);
        var count = groupSize * (int)threadGroupSize;

        clumpsArray = new GrassClump[count];

        for (var i = 0; i < count; i++)
        {
            var pos = new Vector3(Random.value * size.x * 2 - size.x, 0, Random.value * size.y * 2 - size.y);
            clumpsArray[i] = new GrassClump(pos);
        }

        clumpsBuffer = new ComputeBuffer(count, SIZE_GRASS_CLUMP);
        clumpsBuffer.SetData(clumpsArray);

        shader.SetBuffer(kernelUpdateGrass, "clumpsBuffer", clumpsBuffer);
        shader.SetFloat("maxLean", maxLean * Mathf.PI / 180);
        shader.SetFloat("trampleRadius", trampleRadius);
        shader.SetFloat("speed", speed);
        timeID = Shader.PropertyToID("time");
        tramplePosID = Shader.PropertyToID("tramplePos");

        argsArray[0] = mesh.GetIndexCount(0);
        argsArray[1] = (uint)count;
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        material.SetBuffer("clumpsBuffer", clumpsBuffer);
        material.SetFloat("_Scale", scale);
    }

    struct GrassClump
    {
        public Vector3 position;
        public float lean;
        public float trample;
        public Quaternion quaternion;
        public readonly float noise;

        public GrassClump(Vector3 pos)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            lean = 0;
            noise = Random.Range(0.5f, 1);
            if (Random.value < 0.5f) noise = -noise;
            trample = 0;
            quaternion = Quaternion.identity;
        }
    }
}