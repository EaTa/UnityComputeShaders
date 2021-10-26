using UnityEngine;

[ExecuteInEditMode]
public class BlurHighlight : BasePP
{
    [Range(1, 50)] public int BlurRadius = 20;
    [Range(0.0f, 100.0f)] public float Radius = 10;
    [Range(0.0f, 100.0f)] public float SoftenEdge = 30;
    [Range(0.0f, 1.0f)] public float Shade = 0.5f;
    public Transform TrackedObject;

    Vector4 center;
    RenderTexture horzOutput = null;
    int kernelHorzPassId;

    protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (Shader == null)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            if (TrackedObject && thisCamera)
            {
                var pos = thisCamera.WorldToScreenPoint(TrackedObject.position);
                center.x = pos.x;
                center.y = pos.y;
                Shader.SetVector("center", center);
            }

            CheckResolution(out var resChange);
            if (resChange) SetProperties();
            DispatchWithSource(ref source, ref destination);
        }
    }

    void OnValidate()
    {
        if (!init)
            Init();

        SetProperties();
    }

    protected override void Init()
    {
        center = new Vector4();
        kernelName = "Highlight";
        kernelHorzPassId = Shader.FindKernel("HorzPass");
        base.Init();
    }

    protected override void CreateTextures()
    {
        base.CreateTextures();

        Shader.SetTexture(kernelHorzPassId, "source", renderedSource);
        CreateTexture(ref horzOutput);

        // Writes to horizontally-blurred texture using the appropriate shader core.
        Shader.SetTexture(kernelHorzPassId, "horzOutput", horzOutput);

        // Take the previous horizontally-blurred render texture and blurs it vertically, as well as
        // creates the un-blurred ring around the specified screen location.  This shader kernel writes to the
        // final 'output' render texture.
        Shader.SetTexture(kernelHandle, "horzOutput", horzOutput);
    }

    protected void SetProperties()
    {
        var rad = Radius / 100.0f * texSize.y;
        Shader.SetFloat("radius", rad);
        Shader.SetFloat("edgeWidth", rad * SoftenEdge / 100.0f);
        Shader.SetFloat("shade", Shade);
        Shader.SetInt("blurRadius", BlurRadius);
    }

    protected override void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        if (!init) return;

        Graphics.Blit(source, renderedSource);

        Shader.Dispatch(kernelHorzPassId, groupSize.x, groupSize.y, 1);
        Shader.Dispatch(kernelHandle, groupSize.x, groupSize.y, 1);

        Graphics.Blit(output, destination);
    }
}