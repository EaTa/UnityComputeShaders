using UnityEngine;

[ExecuteInEditMode]
public class Challenge3 : BaseCompletePP
{
    [Range(0.0f, 1.0f)] public float height = 0.3f;

    [Range(0.0f, 100.0f)] public float softenEdge;

    [Range(0.0f, 1.0f)] public float shade;

    [Range(0.0f, 1.0f)] public float tintStrength;

    public Color tintColor = Color.white;

    Vector4 center;

    protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!init || shader == null)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            CheckResolution(out _);
            DispatchWithSource(ref source, ref destination);
        }
    }

    void OnValidate()
    {
        if (!init)
            Init();

        SetProperties();
    }

    protected void SetProperties()
    {
        var tintHeight = height * texSize.y;
        shader.SetFloat("tintHeight", tintHeight);
        shader.SetFloat("edgeWidth", tintHeight * softenEdge / 100.0f);
        shader.SetFloat("shade", shade);
        shader.SetFloat("tintStrength", tintStrength);
        shader.SetVector("tintColor", tintColor);
    }
}