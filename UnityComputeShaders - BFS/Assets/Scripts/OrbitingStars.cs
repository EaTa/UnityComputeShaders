using UnityEngine;

public class OrbitingStars : MonoBehaviour
{
    public int starCount = 17;
    public ComputeShader shader;

    public GameObject prefab;
    int groupSizeX;

    int kernelHandle;

    Transform[] stars;
    uint threadGroupSizeX;

    void Start()
    {
        kernelHandle = shader.FindKernel("OrbitingStars");
        shader.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSizeX, out _, out _);
        groupSizeX = (int)((starCount + threadGroupSizeX - 1) / threadGroupSizeX);

        stars = new Transform[starCount];
        for (var i = 0; i < starCount; i++) stars[i] = Instantiate(prefab, transform).transform;
    }

    void Update()
    {
    }
}