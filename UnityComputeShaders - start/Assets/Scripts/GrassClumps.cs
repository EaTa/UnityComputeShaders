using UnityEngine;

public class GrassClumps : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public ComputeShader shader;

    [Range(0, 1)] public float density = 0.8f;

    [Range(0.1f, 3)] public float scale = 0.2f;

    [Range(10, 45)] public float maxLean = 25;

    uint[] argsArray = { 0, 0, 0, 0, 0 };
    ComputeBuffer argsBuffer;
    Bounds bounds;
    GrassClump[] clumpsArray;

    ComputeBuffer clumpsBuffer;
    int groupSize;
    int kernelLeanGrass;
    int SIZE_GRASS_CLUMP = 5 * sizeof(float);
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
        if (clumpsBuffer != null) clumpsBuffer.Release();
        if (argsBuffer != null) argsBuffer.Release();
    }

    void InitShader()
    {
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