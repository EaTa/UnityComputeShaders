using UnityEngine;

public class MeshDeform : MonoBehaviour
{
    public ComputeShader shader;

    [Range(0.5f, 2.0f)] public float radius;

    int kernelHandle;
    Mesh mesh;

    // Use this for initialization
    void Start()
    {
        if (InitData()) InitShader();
    }

    void Update()
    {
        if (shader)
        {
            shader.SetFloat("radius", radius);
            var delta = (Mathf.Sin(Time.time) + 1) / 2;
            shader.SetFloat("delta", delta);
            shader.Dispatch(kernelHandle, 1, 1, 1);

            GetVerticesFromGPU();
        }
    }

    void OnDestroy()
    {
    }

    bool InitData()
    {
        kernelHandle = shader.FindKernel("CSMain");

        var mf = GetComponent<MeshFilter>();

        if (mf == null)
        {
            Debug.Log("No MeshFilter found");
            return false;
        }

        InitVertexArrays(mf.mesh);
        InitGPUBuffers();

        mesh = mf.mesh;

        return true;
    }

    void InitShader()
    {
        shader.SetFloat("radius", radius);
    }

    void InitVertexArrays(Mesh mesh)
    {
    }

    void InitGPUBuffers()
    {
    }

    void GetVerticesFromGPU()
    {
    }
}