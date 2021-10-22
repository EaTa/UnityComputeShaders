using UnityEngine;
using UnityEngine.Serialization;

public class PassData : MonoBehaviour
{
    [FormerlySerializedAs("shader")] public ComputeShader Shader;
    [FormerlySerializedAs("texResolution")] public int TexResolution = 1024;

    [FormerlySerializedAs("clearColor")] public Color ClearColor;
    [FormerlySerializedAs("circleColor")] public Color CircleColor;

    int circlesHandle;
    int clearHandle;
    RenderTexture outputTexture;

    Renderer rend;

    // Use this for initialization
    void Start()
    {
        outputTexture = new RenderTexture(TexResolution, TexResolution, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitShader();
    }

    void Update()
    {
        DispatchKernels(10);
    }

    void InitShader()
    {
        circlesHandle = Shader.FindKernel("Circles");
        clearHandle = Shader.FindKernel("Clear");

        Shader.SetInt("texResolution", TexResolution);
        Shader.SetVector("circleColor", CircleColor);
        Shader.SetVector("clearColor", ClearColor);

        Shader.SetTexture(circlesHandle, "Result", outputTexture);
        Shader.SetTexture(clearHandle, "Result", outputTexture);

        rend.material.SetTexture("_MainTex", outputTexture);
    }

    void DispatchKernels(int count)
    {
        Shader.Dispatch(clearHandle, TexResolution / 8, TexResolution / 8, 1);
        Shader.SetFloat("time", Time.time);
        Shader.Dispatch(circlesHandle, count, 1, 1);
    }
}