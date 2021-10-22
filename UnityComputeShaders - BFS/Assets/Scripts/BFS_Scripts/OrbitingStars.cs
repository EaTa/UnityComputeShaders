using System;
using UnityEngine;

public class OrbitingStars : MonoBehaviour
{
    public int StarCount = 1000;
    public ComputeShader Shader;

    public GameObject Prefab;

    Transform[] stars;

    int groupSizeX;
    uint threadGroupSizeX;
    int kernelHandle;
    ComputeBuffer resultBuffer;
    Vector3[] output;

    void Start()
    {
        if (!Shader) return;

        kernelHandle = Shader.FindKernel("OrbitingStars");
        Shader.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSizeX, out _, out _);
        groupSizeX = (int)((StarCount + threadGroupSizeX - 1) / threadGroupSizeX);

        // GPU-side data buffer
        resultBuffer = new ComputeBuffer(StarCount, sizeof(float) * 3);
        Shader.SetBuffer(kernelHandle, "Result", resultBuffer);
        output = new Vector3[StarCount];

        stars = new Transform[StarCount];
        for (var i = 0; i < StarCount; i++)
            stars[i] = Instantiate(Prefab, transform).transform;
    }

    void Update()
    {
        Shader.SetFloat("time", Time.time);
        Shader.Dispatch(kernelHandle, groupSizeX, 1, 1);

        // Harvest star position data from GPU and use it to position the star prefabs.
        resultBuffer.GetData(output);
        var starCount = 0;
        foreach (var starPosition in stars)
        {
            starPosition.localPosition = output[starCount];
            starCount++;
        }

    }

    void OnDestroy()
    {
        resultBuffer.Dispose();
    }
}