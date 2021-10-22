using UnityEngine;
using UnityEngine.Assertions;

public class AssignTexture : MonoBehaviour
{
    public enum ShaderKernelName
    {
        None = 0,
        CSMain = 1,
        SolidRed = 2,
        SolidYellow = 3,
        SplitScreen = 4,
        Circle = 5,
        Square = 6
    }
    public ComputeShader UserShader;
    public ShaderKernelName KernelName = ShaderKernelName.CSMain;
    public int TexResolution = 256;

    Renderer rend;
    RenderTexture outputTexture;
    int kernalHandle;
    static readonly int mainTex = Shader.PropertyToID("_MainTex");

    // Start is called before the first frame update
    void Start()
    {
        outputTexture = new RenderTexture(TexResolution, TexResolution, 0)
        {
            enableRandomWrite = true
        };
        Assert.IsTrue(outputTexture.Create());

        rend = GetComponent<Renderer>();
        Assert.IsNotNull(rend);
        rend.enabled = true;

        InitShader();
    }

    void InitShader()
    {
        kernalHandle = UserShader.FindKernel(KernelName.ToString());

        UserShader.SetTexture(kernalHandle, "Result", outputTexture);
        UserShader.SetInt("texResolution", TexResolution);

        rend.material.SetTexture(mainTex, outputTexture);

        DispatchShader(TexResolution / 8, TexResolution / 8);

    }

    void DispatchShader(int x, int y)
    {
        UserShader.Dispatch(kernalHandle, x, y, 1);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.U))
        {
            DispatchShader(TexResolution / 8, TexResolution / 8);
        }
    }
}