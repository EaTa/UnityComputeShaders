using UnityEngine;
using UnityEngine.Serialization;

public class ProceduralWood : MonoBehaviour
{
    public ComputeShader Shader;
    public int TexResolution = 256;

    public Color PaleColor = new Color(0.733f, 0.565f, 0.365f, 1);
    public Color DarkColor = new Color(0.49f, 0.286f, 0.043f, 1);
    public float Frequency = 2.0f;
    public float NoiseScale = 6.0f;
    public float RingScale = 0.6f;
    public float Contrast = 4.0f;

    int kernelHandle;
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
        if (Input.GetKeyUp(KeyCode.U)) DispatchShader(TexResolution / 8, TexResolution / 8);
    }

    void InitShader()
    {
        kernelHandle = Shader.FindKernel("CSMain");

        Shader.SetInt("texResolution", TexResolution);

        Shader.SetVector("paleColor", PaleColor);
        Shader.SetVector("darkColor", DarkColor);
        Shader.SetFloat("frequency", Frequency);
        Shader.SetFloat("noiseScale", NoiseScale);
        Shader.SetFloat("ringScale", RingScale);
        Shader.SetFloat("contrast", Contrast);

        Shader.SetTexture(kernelHandle, "Result", outputTexture);

        rend.material.SetTexture("_MainTex", outputTexture);

        DispatchShader(TexResolution / 8, TexResolution / 8);
    }

    void DispatchShader(int x, int y)
    {
        Shader.Dispatch(kernelHandle, x, y, 1);
    }
}