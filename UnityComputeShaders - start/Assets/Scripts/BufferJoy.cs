using UnityEngine;

public class BufferJoy : MonoBehaviour
{
    public ComputeShader shader;
    public int texResolution = 1024;

    public Color clearColor;
    public Color circleColor;

    int circlesHandle;
    int clearHandle;

    readonly int count = 10;
    RenderTexture outputTexture;

    Renderer rend;

    // Use this for initialization
    void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0);
        outputTexture.enableRandomWrite = true;
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
        circlesHandle = shader.FindKernel("Circles");
    }

    void InitShader()
    {
        clearHandle = shader.FindKernel("Clear");

        shader.SetVector("clearColor", clearColor);
        shader.SetVector("circleColor", circleColor);
        shader.SetInt("texResolution", texResolution);

        shader.SetTexture(clearHandle, "Result", outputTexture);
        shader.SetTexture(circlesHandle, "Result", outputTexture);

        rend.material.SetTexture("_MainTex", outputTexture);
    }

    void DispatchKernels(int count)
    {
        shader.Dispatch(clearHandle, texResolution / 8, texResolution / 8, 1);
        shader.SetFloat("time", Time.time);
        shader.Dispatch(circlesHandle, count, 1, 1);
    }
}