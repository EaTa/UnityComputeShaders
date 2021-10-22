using UnityEngine;
using UnityEngine.Serialization;

public class BufferJoy : MonoBehaviour
{
    [FormerlySerializedAs("shader")] public ComputeShader Shader;
    [FormerlySerializedAs("texResolution")] public int TexResolution = 1024;

    [FormerlySerializedAs("clearColor")] public Color ClearColor;
    [FormerlySerializedAs("circleColor")] public Color CircleColor;

    int circlesHandle;
    int clearHandle;

    struct Circle
    {
        public Vector2 origin;
        public Vector2 velocity;
        public float radius;
    }

    const int count = 10;

    Circle[] circleData;    // cpu data
    ComputeBuffer buffer;

    RenderTexture outputTexture;
    Renderer rend;
    static readonly int mainTex = UnityEngine.Shader.PropertyToID("_MainTex");

    // Use this for initialization
    void Start()
    {
        outputTexture = new RenderTexture(TexResolution, TexResolution, 0)
        {
            enableRandomWrite = true
        };
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitData();

        InitShader();
    }

    void Update()
    {
        DispatchKernels(count);
    }

    void InitData()
    {
        circlesHandle = Shader.FindKernel("Circles");

        Shader.GetKernelThreadGroupSizes(circlesHandle, out var threadGroupsSizeX, out _, out _);

        var total = (int)threadGroupsSizeX * count;
        circleData = new Circle[total];

        const float speed = 100f;
        const float halfSpeed = speed * 0.5f;
        const float minRadius = 10f;
        const float maxRadius = 30f;
        const float radiusRange = maxRadius - minRadius;

        for (var i = 0; i < total; i++)
        {
            var circle = circleData[i];
            circle.origin.x = Random.value * TexResolution;
            circle.origin.y = Random.value * TexResolution;
            circle.velocity.x = (Random.value * speed) - halfSpeed;
            circle.velocity.y = (Random.value * speed) - halfSpeed;
            circle.radius = Random.value * radiusRange + minRadius;
            circleData[i] = circle;
        }
    }

    void InitShader()
    {
        clearHandle = Shader.FindKernel("Clear");

        Shader.SetVector("clearColor", ClearColor);
        Shader.SetVector("circleColor", CircleColor);
        Shader.SetInt("texResolution", TexResolution);

        const int stride = (2 + 2 + 1) * sizeof(float); //2 floats origin, 2 floats velocity, 1 float radius - 4 bytes per float
        buffer = new ComputeBuffer(circleData.Length, stride);
        buffer.SetData(circleData);
        Shader.SetBuffer(circlesHandle, "circlesBuffer", buffer);

        Shader.SetTexture(clearHandle, "Result", outputTexture);
        Shader.SetTexture(circlesHandle, "Result", outputTexture);

        rend.material.SetTexture(mainTex, outputTexture);
    }

    void DispatchKernels(int count)
    {
        Shader.Dispatch(clearHandle, TexResolution / 8, TexResolution / 8, 1);
        Shader.SetFloat("time", Time.time);
        Shader.Dispatch(circlesHandle, count, 1, 1);
    }

    void OnDestroy()
    {
        buffer.Dispose();
    }
}