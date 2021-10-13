using UnityEngine;

public class SimpleFlocking : MonoBehaviour
{
    public ComputeShader shader;

    public float rotationSpeed = 1f;
    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    public float boidSpeedVariation = 1f;
    public GameObject boidPrefab;
    public int boidsCount;
    public float spawnRadius;
    public Transform target;
    GameObject[] boids;
    Boid[] boidsArray;
    ComputeBuffer boidsBuffer;
    int groupSizeX;

    int kernelHandle;
    int numOfBoids;

    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        uint x;
        shader.GetKernelThreadGroupSizes(kernelHandle, out x, out _, out _);
        groupSizeX = Mathf.CeilToInt(boidsCount / (float)x);
        numOfBoids = groupSizeX * (int)x;

        InitBoids();
        InitShader();
    }

    void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);

        boidsBuffer.GetData(boidsArray);

        for (var i = 0; i < boidsArray.Length; i++)
        {
            boids[i].transform.localPosition = boidsArray[i].position;

            if (!boidsArray[i].direction.Equals(Vector3.zero))
                boids[i].transform.rotation = Quaternion.LookRotation(boidsArray[i].direction);
        }
    }

    void OnDestroy()
    {
        if (boidsBuffer != null) boidsBuffer.Dispose();
    }

    void InitBoids()
    {
        boids = new GameObject[numOfBoids];
        boidsArray = new Boid[numOfBoids];

        for (var i = 0; i < numOfBoids; i++)
        {
            var pos = transform.position + Random.insideUnitSphere * spawnRadius;
            boidsArray[i] = new Boid(pos);
            boids[i] = Instantiate(boidPrefab, pos, Quaternion.identity);
            boidsArray[i].direction = boids[i].transform.forward;
        }
    }

    void InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, 6 * sizeof(float));
        boidsBuffer.SetData(boidsArray);

        shader.SetBuffer(kernelHandle, "boidsBuffer", boidsBuffer);
        shader.SetFloat("rotationSpeed", rotationSpeed);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetInt("boidsCount", boidsCount);
    }

    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;

        public Boid(Vector3 pos)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            direction.x = 0;
            direction.y = 0;
            direction.z = 0;
        }
    }
}