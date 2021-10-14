using UnityEngine;

public class GrassBlades : MonoBehaviour
{
    public Material material;
    public ComputeShader shader;
    public Material visualizeNoise;
    public bool viewNoise;

    [Range(0, 1)] public float density;

    [Range(0.1f, 3)] public float scale;

    [Range(10, 45)] public float maxBend;

    [Range(0, 2)] public float windSpeed;

    [Range(0, 360)] public float windDirection;

    [Range(10, 1000)] public float windScale;

    readonly uint[] argsArray = { 0, 0, 0, 0, 0 };
    ComputeBuffer argsBuffer;
    Mesh blade;
    GrassBlade[] bladesArray;

    ComputeBuffer bladesBuffer;
    Bounds bounds;
    Material groundMaterial;
    int groupSize;
    int kernelBendGrass;
    readonly int SIZE_GRASS_BLADE = 6 * sizeof(float);
    int timeID;

    Mesh Blade
    {
        get
        {
            Mesh mesh;

            if (blade != null)
            {
                mesh = blade;
            }
            else
            {
                mesh = new Mesh();

                var height = 0.2f;
                var rowHeight = height / 4;
                var halfWidth = height / 10;

                //1. Use the above variables to define the vertices array

                //2. Define the normals array, hint: each vertex uses the same normal
                var normal = new Vector3(0, 0, -1);

                //3. Define the uvs array

                //4. Define the indices array

                //5. Assign the mesh properties using the arrays
                //   for indices use
                //   mesh.SetIndices( indices, MeshTopology.Triangles, 0 );
            }

            return mesh;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        bounds = new Bounds(Vector3.zero, new Vector3(30, 30, 30));
        blade = Blade;

        var renderer = GetComponent<MeshRenderer>();
        groundMaterial = renderer.material;

        InitShader();
    }

    // Update is called once per frame
    void Update()
    {
        shader.SetFloat(timeID, Time.time);
        shader.Dispatch(kernelBendGrass, groupSize, 1, 1);

        if (!viewNoise) Graphics.DrawMeshInstancedIndirect(blade, 0, material, bounds, argsBuffer);
    }

    void OnDestroy()
    {
        bladesBuffer.Release();
        argsBuffer.Release();
    }

    void OnValidate()
    {
        if (groundMaterial != null)
        {
            var renderer = GetComponent<MeshRenderer>();

            renderer.material = viewNoise ? visualizeNoise : groundMaterial;

            //TO DO: set wind using wind direction, speed and noise scale
            var wind = new Vector4();
            shader.SetVector("wind", wind);
            visualizeNoise.SetVector("wind", wind);
        }
    }

    void InitShader()
    {
        var mf = GetComponent<MeshFilter>();
        var bounds = mf.sharedMesh.bounds;

        var blades = bounds.extents;
        var vec = transform.localScale / 0.1f * density;
        blades.x *= vec.x;
        blades.z *= vec.z;

        var total = (int)blades.x * (int)blades.z * 20;

        kernelBendGrass = shader.FindKernel("BendGrass");

        uint threadGroupSize;
        shader.GetKernelThreadGroupSizes(kernelBendGrass, out threadGroupSize, out _, out _);
        groupSize = Mathf.CeilToInt(total / (float)threadGroupSize);
        var count = groupSize * (int)threadGroupSize;

        bladesArray = new GrassBlade[count];

        for (var i = 0; i < count; i++)
        {
            var pos = new Vector3(Random.value * bounds.extents.x * 2 - bounds.extents.x + bounds.center.x,
                0,
                Random.value * bounds.extents.z * 2 - bounds.extents.z + bounds.center.z);
            pos = transform.TransformPoint(pos);
            bladesArray[i] = new GrassBlade(pos);
        }

        bladesBuffer = new ComputeBuffer(count, SIZE_GRASS_BLADE);
        bladesBuffer.SetData(bladesArray);

        shader.SetBuffer(kernelBendGrass, "bladesBuffer", bladesBuffer);
        shader.SetFloat("maxBend", maxBend * Mathf.PI / 180);
        //TO DO: set wind using wind direction, speed and noise scale
        var wind = new Vector4();
        shader.SetVector("wind", wind);

        timeID = Shader.PropertyToID("time");

        argsArray[0] = blade.GetIndexCount(0);
        argsArray[1] = (uint)count;
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        material.SetBuffer("bladesBuffer", bladesBuffer);
        material.SetFloat("_Scale", scale);
    }

    struct GrassBlade
    {
        public Vector3 position;
        public float bend;
        public float noise;
        public float fade;

        public GrassBlade(Vector3 pos)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            bend = 0;
            noise = Random.Range(0.5f, 1) * 2 - 1;
            fade = Random.Range(0.5f, 1);
        }
    }
}