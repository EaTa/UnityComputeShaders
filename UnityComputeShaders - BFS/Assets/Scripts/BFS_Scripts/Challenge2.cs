using UnityEngine;
using UnityEngine.Assertions;

public class Challenge2 : MonoBehaviour
{
    public ComputeShader Shader;
    public int TexResolution = 1024;

    [Range(0.1f, 0.5f)] public float Radius = 0.15f;
    [Range(3, 12)] public int Sides = 8;
    [Range(0.01f, 1f)] public float RotationSpeed = 0.1f;
    public Color FillColor = new Color(1.0f, 1.0f, 0.0f, 1.0f);
    public Color ClearColor = new Color(0, 0, 0.3f, 1.0f);
    [Range(0.001f, 0.1f)] public float EdgeBlur = 0.001f;

    int polygonHandle;

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
        Assert.IsNotNull(rend);
        rend.enabled = true;

        InitShader();
    }

    void Update()
    {
        // Compute shader kernel dispatch.
        DispatchShader(TexResolution / 8, TexResolution / 8);
    }

    void InitShader()
    {
        polygonHandle = Shader.FindKernel("CSMain");

        Shader.SetInt("texResolution", TexResolution);

        Shader.SetTexture(polygonHandle, "Result", outputTexture);
        rend.material.SetTexture(mainTex, outputTexture);
    }

    void DispatchShader(int x, int y)
    {
        Shader.SetFloat("time", Time.time);
        Shader.SetVector("fillColor", FillColor);
        Shader.SetVector("clearColor", ClearColor);
        Shader.SetFloat("radius", Radius);
        Shader.SetInt("sides", Sides);
        Shader.SetFloat("speed", RotationSpeed);
        Shader.SetFloat("edge", EdgeBlur);

        Shader.Dispatch(polygonHandle, x, y, 1);
    }
}