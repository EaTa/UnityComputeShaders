using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

public class Challenge1 : MonoBehaviour
{
    [Header("Compute Shader Properties")]
    public ComputeShader Shader;
    public int TextureResolution = 1024;

    [Header("Rectangle Properties")]
    public Vector2Int RectCenter = Vector2Int.zero;
    public Vector2Int RectWidthHeight = Vector2Int.one;

    int kernelHandle;
    RenderTexture outputTexture;
    Vector4 rectangleDef;

    Renderer rend;
    static readonly int mainTex = UnityEngine.Shader.PropertyToID("_MainTex");

    // Use this for initialization
    void Start()
    {
        outputTexture = new RenderTexture(TextureResolution, TextureResolution, 0)
        {
            enableRandomWrite = true
        };
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        Assert.IsNotNull(rend);
        rend.enabled = true;

        InitShader();
    }

    void InitShader()
    {
        kernelHandle = Shader.FindKernel("Rectangle");

        Shader.SetTexture(kernelHandle, "Result", outputTexture);
        Shader.SetInt("TexResolution", TextureResolution);

        // Create a Vector4 with parameters x, y, width, height
        rectangleDef = new Vector4(RectCenter.x, RectCenter.y, RectWidthHeight.x, RectWidthHeight.y);

        // Pass this to the shader using SetVector
        Shader.SetVector("RectangleDef", rectangleDef);

        rend.material.SetTexture(mainTex, outputTexture);

        DispatchShader(TextureResolution / 8, TextureResolution / 8);
    }

    void DispatchShader(int x, int y)
    {
        Shader.Dispatch(kernelHandle, x, y, 1);
    }

    void Update()
    {
        // Update shader properties.
        if (Input.GetKeyUp(KeyCode.U))
        {
            Shader.SetInt("TexResolution", TextureResolution);
            rectangleDef = new Vector4(RectCenter.x, RectCenter.y, RectWidthHeight.x, RectWidthHeight.y);
            Shader.SetVector("RectangleDef", rectangleDef);
            DispatchShader(TextureResolution / 8, TextureResolution / 8);
        }
    }
}