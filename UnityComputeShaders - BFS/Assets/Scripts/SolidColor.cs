using UnityEngine;

public class SolidColor : MonoBehaviour
{
    public ComputeShader shader;
    public int texResolution = 256;

    int kernelHandle;
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
        if (Input.GetKeyUp(KeyCode.U)) DispatchShader(texResolution / 8, texResolution / 8);
    }

    void InitShader()
    {
        kernelHandle = shader.FindKernel("CSMain");

        shader.SetTexture(kernelHandle, "Result", outputTexture);

        rend.material.SetTexture("_MainTex", outputTexture);

        DispatchShader(texResolution / 8, texResolution / 8);
    }

    void DispatchShader(int x, int y)
    {
        shader.Dispatch(kernelHandle, x, y, 1);
    }
}