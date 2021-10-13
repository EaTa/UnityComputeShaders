using UnityEngine;

public class Challenge4 : MonoBehaviour
{
    public ComputeShader shader;

    public float rotationSpeed = 1f;
    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    public float boidSpeedVariation = 1f;
    public Mesh boidMesh;
    public Material boidMaterial;
    public int boidsCount;
    public float spawnRadius;
    public Transform target;
    readonly uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    ComputeBuffer argsBuffer;
    GameObject[] boids;
    Boid[] boidsArray;
    ComputeBuffer boidsBuffer;
    Bounds bounds;
    int groupSizeX;

    int kernelHandle;
    int numOfBoids;
    MaterialPropertyBlock props;

    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        uint x;
        shader.GetKernelThreadGroupSizes(kernelHandle, out x, out _, out _);
        groupSizeX = Mathf.CeilToInt(boidsCount / (float)x);
        numOfBoids = groupSizeX * (int)x;

        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        props = new MaterialPropertyBlock();
        props.SetFloat("_UniqueID", Random.value);

        InitBoids();
        InitShader();

        //Debug.Log(boidMesh.bounds);
    }

    void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);

        Graphics.DrawMeshInstancedIndirect(boidMesh, 0, boidMaterial, bounds, argsBuffer, 0, props);
    }

    void OnDestroy()
    {
        if (boidsBuffer != null) boidsBuffer.Dispose();

        if (argsBuffer != null) argsBuffer.Dispose();
    }

    void InitBoids()
    {
        boids = new GameObject[numOfBoids];
        boidsArray = new Boid[numOfBoids];

        for (var i = 0; i < numOfBoids; i++)
        {
            var pos = transform.position + Random.insideUnitSphere * spawnRadius;
            var rot = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
            var offset = Random.value * 1000.0f;
            boidsArray[i] = new Boid(pos, rot.eulerAngles, offset);
        }
    }

    void InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, 8 * sizeof(float));
        boidsBuffer.SetData(boidsArray);

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (boidMesh != null)
        {
            args[0] = boidMesh.GetIndexCount(0);
            args[1] = (uint)numOfBoids;
        }

        argsBuffer.SetData(args);

        shader.SetBuffer(kernelHandle, "boidsBuffer", boidsBuffer);
        shader.SetFloat("rotationSpeed", rotationSpeed);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetInt("boidsCount", numOfBoids);

        boidMaterial.SetBuffer("boidsBuffer", boidsBuffer);
    }

    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;
        public float noise_offset;
        public float theta;

        public Boid(Vector3 pos, Vector3 dir, float offset)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            direction.x = dir.x;
            direction.y = dir.y;
            direction.z = dir.z;
            noise_offset = offset;
            theta = Random.value * Mathf.PI * 2;
        }
    }
}