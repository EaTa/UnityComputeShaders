using UnityEngine;
using UnityEngine.Serialization;

public class MeshDeform : MonoBehaviour
{
    [FormerlySerializedAs("shader")] public ComputeShader Shader;
    [FormerlySerializedAs("radius")] [Range(0.5f, 2.0f)] public float Radius;

    int kernelHandle;
    Mesh mesh;

    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;

        public Vertex(Vector3 p, Vector3 n)
        {
            position.x = p.x;
            position.y = p.y;
            position.z = p.z;
            normal.x = n.x;
            normal.y = n.y;
            normal.z = n.z;
        }
    }

    Vertex[] vertexArray;
    Vertex[] initialArray;
    ComputeBuffer vertexBuffer;
    ComputeBuffer initialBuffer;

    // Use this for initialization
    void Start()
    {
        if (InitData())
            InitShader();
    }

    void Update()
    {
        if (!Shader) return;

        Shader.SetFloat("radius", Radius);
        var delta = (Mathf.Sin(Time.time) + 1) / 2;
        Shader.SetFloat("delta", delta);

        Shader.Dispatch(kernelHandle, vertexArray.Length, 1, 1);

        GetVerticesFromGPU();
    }

    void OnDestroy()
    {
        vertexBuffer.Dispose();
        initialBuffer.Dispose();
    }

    bool InitData()
    {
        kernelHandle = Shader.FindKernel("CSMain");

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
        Shader.SetFloat("radius", Radius);
    }

    void InitVertexArrays(Mesh mesh)
    {
        vertexArray = new Vertex[mesh.vertexCount];
        initialArray = new Vertex[mesh.vertexCount];

        for (var i = 0; i < vertexArray.Length; i++)
        {
            vertexArray[i] = new Vertex(mesh.vertices[i], mesh.normals[i]);
            initialArray[i] = new Vertex(mesh.vertices[i], mesh.normals[i]);
        }
    }

    void InitGPUBuffers()
    {
        vertexBuffer = new ComputeBuffer(vertexArray.Length, sizeof(float) * 6);
        vertexBuffer.SetData(vertexArray);
        initialBuffer = new ComputeBuffer(initialArray.Length, sizeof(float) * 6);
        initialBuffer.SetData(initialArray);

        Shader.SetBuffer(kernelHandle, "vertexBuffer", vertexBuffer);
        Shader.SetBuffer(kernelHandle, "initialBuffer", initialBuffer);
    }

    void GetVerticesFromGPU()
    {
        vertexBuffer.GetData(vertexArray);
        var vertices = new Vector3[vertexBuffer.count];
        var normals = new Vector3[vertexBuffer.count];
        for (var i = 0; i < vertexBuffer.count; i++)
        {
            vertices[i] = vertexArray[i].position;
            normals[i] = vertexArray[i].normal;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
    }
}