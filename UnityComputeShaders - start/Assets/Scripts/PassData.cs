using UnityEngine;

public class PassData : MonoBehaviour
{
    public ComputeShader shader;
    public int texResolution = 1024;

    public Color clearColor;
    public Color circleColor;

    int circlesHandle;
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

        InitShader();
    }

    void Update()
    {
        DispatchKernel(1);
    }

    void InitShader()
    {
        circlesHandle = shader.FindKernel("Circles");

        shader.SetInt("texResolution", texResolution);
        shader.SetTexture(circlesHandle, "Result", outputTexture);

        rend.material.SetTexture("_MainTex", outputTexture);
    }

    void DispatchKernel(int count)
    {
        shader.Dispatch(circlesHandle, count, 1, 1);
    }
}