using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BasePP : MonoBehaviour
{
    [SerializeField] protected ComputeShader Shader;

    protected Vector2Int groupSize;
    protected bool init;

    protected int kernelHandle = -1;

    protected string kernelName = "CSMain";

    protected RenderTexture output;
    protected RenderTexture renderedSource;

    protected Vector2Int texSize = new Vector2Int(0, 0);
    protected Camera thisCamera;

    protected virtual void OnEnable()
    {
        Init();
    }

    protected virtual void OnDisable()
    {
        ClearTextures();
        init = false;
    }

    protected virtual void OnDestroy()
    {
        ClearTextures();
        init = false;
    }

    protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!init || Shader == null)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            CheckResolution(out _);
            DispatchWithSource(ref source, ref destination);
        }
    }

    protected virtual void Init()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("It seems your target Hardware does not support Compute Shaders.");
            return;
        }

        if (!Shader)
        {
            Debug.LogError("No shader");
            return;
        }

        kernelHandle = Shader.FindKernel(kernelName);

        thisCamera = GetComponent<Camera>();

        if (!thisCamera)
        {
            Debug.LogError("Object has no Camera");
            return;
        }

        CreateTextures();

        init = true;
    }

    protected void ClearTexture(ref RenderTexture textureToClear)
    {
        if (null == textureToClear) return;

        textureToClear.Release();
        textureToClear = null;
    }

    protected virtual void ClearTextures()
    {
        ClearTexture(ref output);
        ClearTexture(ref renderedSource);
    }

    protected void CreateTexture(ref RenderTexture textureToMake, int divide = 1)
    {
        textureToMake = new RenderTexture(texSize.x / divide, texSize.y / divide, 0)
        {
            enableRandomWrite = true
        };
        textureToMake.Create();
    }


    protected virtual void CreateTextures()
    {
        if (!Shader) return;

        texSize.x = thisCamera.pixelWidth;
        texSize.y = thisCamera.pixelHeight;

        Shader.GetKernelThreadGroupSizes(kernelHandle, out var x, out var y, out _);
        groupSize.x = Mathf.CeilToInt(texSize.x / (float)x);
        groupSize.y = Mathf.CeilToInt(texSize.y / (float)y);

        CreateTexture(ref output);
        CreateTexture(ref renderedSource);

        Shader.SetTexture(kernelHandle, "source", renderedSource);
        Shader.SetTexture(kernelHandle, "output", output);
    }

    protected virtual void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        Graphics.Blit(source, renderedSource);
        Shader.Dispatch(kernelHandle, groupSize.x, groupSize.y, 1);
        Graphics.Blit(output, destination);
    }

    protected void CheckResolution(out bool resChange)
    {
        resChange = false;

        if (texSize.x == thisCamera.pixelWidth && texSize.y == thisCamera.pixelHeight) return;

        resChange = true;
        CreateTextures();
    }
}