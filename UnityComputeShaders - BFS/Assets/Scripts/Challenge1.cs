using UnityEngine;

public class Challenge1 : MonoBehaviour
{
    public ComputeShader shader;
    public int texResolution = 1024;

    int kernelHandle;
    RenderTexture outputTexture;

    Renderer rend;
    static readonly int MainTex = Shader.PropertyToID("_MainTex");

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
        kernelHandle = shader.FindKernel("Square");

        //Create a Vector4 with parameters x, y, width, height
        //Pass this to the shader using SetVector

        shader.SetTexture(kernelHandle, "Result", outputTexture);

        rend.material.SetTexture(MainTex, outputTexture);

        DispatchShader(texResolution / 8, texResolution / 8);
    }

    void DispatchShader(int x, int y)
    {
        shader.Dispatch(kernelHandle, x, y, 1);
    }
}