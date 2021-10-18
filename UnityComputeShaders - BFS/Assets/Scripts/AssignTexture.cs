using UnityEngine;
using UnityEngine.Assertions;

public class AssignTexture : MonoBehaviour
{
    public ComputeShader UserShader;
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
        kernalHandle = UserShader.FindKernel("CSMain");
        UserShader.SetTexture(kernalHandle, "Result", outputTexture);
        rend.material.SetTexture(mainTex, outputTexture);

        DispatchShader(TexResolution / 16, TexResolution / 16);

    }

    void DispatchShader(int x, int y)
    {
        UserShader.Dispatch(kernalHandle, x, y, 1);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.F))
        {
            DispatchShader(TexResolution / 8, TexResolution / 8);
        }
    }
}