using UnityEngine;
using UnityEngine.SocialPlatforms.GameCenter;

[ExecuteInEditMode]
public class RingHighlight : BasePP
{
    [Range(0.0f, 100.0f)] public float Radius = 10;
    [Range(0.0f, 100.0f)] public float SoftenEdge;
    [Range(0.0f, 1.0f)] public float Shade;

    public Transform TrackedObject;

    Vector4 center;

    protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!init || Shader == null)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            if (!TrackedObject)
            {
                Debug.Log($"{GetType()} :: OnRenderImage() -- No tracked object to highlight!");
                Graphics.Blit(source, destination);
                return;
            }

            var pos = thisCamera.WorldToScreenPoint(TrackedObject.position);
            center.x = pos.x;
            center.y = pos.y;
            Shader.SetVector("center", center);
            CheckResolution(out var resChange);
            if (resChange) SetShaderProperties();
            DispatchWithSource(ref source, ref destination);
        }
    }

    void OnValidate()
    {
        if (!init)
            Init();

        SetShaderProperties();
    }

    protected override void Init()
    {
        kernelName = "Highlight";
        base.Init();
    }

    void SetShaderProperties()
    {
        var rad = Radius / 100.0f * texSize.y;
        Shader.SetFloat("radius", rad);
        Shader.SetFloat("edgeWidth", rad * SoftenEdge / 100.0f);
        Shader.SetFloat("shade", Shade);
    }
}