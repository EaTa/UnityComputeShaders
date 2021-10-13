// StableFluids - A GPU implementation of Jos Stam's Stable Fluids on Unity
// adapted from https://github.com/keijiro/StableFluids

using UnityEngine;

public class StableFluids : MonoBehaviour
{
    public int resolution = 512;
    public float viscosity = 1e-6f;
    public float force = 300;
    public float exponent = 200;
    public Texture2D initial;
    public ComputeShader compute;
    public Material material;

    // Color buffers (for double buffering)
    RenderTexture colorRT1;
    RenderTexture colorRT2;

    int kernelAdvect;
    int kernelDiffuse1;
    int kernelDiffuse2;
    int kernelForce;
    int kernelProject;
    int kernelProjectSetup;

    Vector2 previousInput;
    RenderTexture vfbRTP1;
    RenderTexture vfbRTP2;

    // Vector field buffers
    RenderTexture vfbRTV1;
    RenderTexture vfbRTV2;
    RenderTexture vfbRTV3;

    int threadCountX => (resolution + 7) / 8;
    int threadCountY => (resolution * Screen.height / Screen.width + 7) / 8;

    int resolutionX => threadCountX * 8;
    int resolutionY => threadCountY * 8;

    void Start()
    {
        InitBuffers();
        InitShader();

        Graphics.Blit(initial, colorRT1);
    }

    void Update()
    {
        var dt = Time.deltaTime;
        var dx = 1.0f / resolutionY;

        // Input point
        var input = new Vector2(
            (Input.mousePosition.x - Screen.width * 0.5f) / Screen.height,
            (Input.mousePosition.y - Screen.height * 0.5f) / Screen.height
        );

        // Common variables
        compute.SetFloat("Time", Time.time);
        compute.SetFloat("DeltaTime", dt);

        //Add code here


        previousInput = input;
    }

    void OnDestroy()
    {
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(colorRT1, destination, material, 1);
    }


    void OnValidate()
    {
        resolution = Mathf.Max(resolution, 8);
    }

    RenderTexture CreateRenderTexture(int componentCount, int width = 0, int height = 0)
    {
        var rt = new RenderTexture(width, height, 0);
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }

    void InitBuffers()
    {
    }

    void InitShader()
    {
    }
}