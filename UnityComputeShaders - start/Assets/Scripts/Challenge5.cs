using System.Collections.Generic;
using CjLib;
using UnityEngine;

public class Challenge5 : MonoBehaviour
{
    public bool debugWireframe;

    public ComputeShader shader;
    public Material rigidBodyMaterial;
    public Material sphereMaterial;
    public Material lineMaterial;
    public Bounds bounds;
    public float cubeMass;
    public float scale;
    public float springCoefficient;
    public float dampingCoefficient;
    public float tangentialCoefficient;
    public float gravityCoefficient;
    public float frictionCoefficient;
    public float angularFrictionCoefficient;
    public float angularForceScalar;
    public float linearForceScalar;
    public Vector3Int gridSize = new Vector3Int(5, 5, 5);
    public Vector3 gridPosition; //centre of grid
    public bool useGrid = true;
    public int rigidBodyCount = 1000;

    [Range(1, 20)] public int stepsPerUpdate = 10;

    int activeCount;
    ComputeBuffer argsBuffer;
    ComputeBuffer argsLineBuffer;
    ComputeBuffer argsSphereBuffer;

    // calculated
    Vector3 cubeScale;
    int deltaTimeID;

    int frameCounter;
    int groupsPerGridCell;
    int groupsPerParticle;

    int groupsPerRigidBody;
    int kernelClearGrid;
    int kernelCollisionDetection;
    int kernelCollisionDetectionWithGrid;
    int kernelComputeMomenta;
    int kernelComputePositionAndRotation;

    int kernelGenerateParticleValues;
    int kernelPopulateGrid;

    Mesh mesh;

    float particleDiameter;

    List<Vector3> particleInitialPositions;
    Particle[] particlesArray;
    ComputeBuffer particlesBuffer;
    int particlesPerBody;

    RigidBody[] rigidBodiesArray;

    ComputeBuffer rigidBodiesBuffer;

    readonly int SIZE_PARTICLE = 15 * sizeof(float);

    readonly int SIZE_RIGIDBODY = 13 * sizeof(float) + 2 * sizeof(int);
    readonly uint vertexCount = 0;
    int[] voxelGridArray;
    ComputeBuffer voxelGridBuffer; // int4*2

    public Mesh sphereMesh => PrimitiveMeshFactory.SphereWireframe(6, 6);

    public Mesh lineMesh => PrimitiveMeshFactory.Line(Vector3.zero, new Vector3(1.0f, 1.0f, 1.0f));

    void Start()
    {
        //1. Get the VoxelizeMesh component
        //2. Use meshToVoxelize as the property mesh
        //3. Use it to voxelize its mesh
        //4. Set the particleInitialPositions to the PositionList
        //5. Set the particlesPerBody
        //6. Set vertex count
        //7. Set the particle diameter

        InitArrays();

        InitRigidBodies();

        InitParticles();

        InitBuffers();

        InitShader();

        InitInstancing();
    }

    void Update()
    {
        if (activeCount < rigidBodyCount && frameCounter++ > 5)
        {
            activeCount++;
            frameCounter = 0;
            shader.SetInt("activeCount", activeCount);
            uint[] args = { vertexCount, (uint)activeCount, 0, 0, 0 };
            argsBuffer.SetData(args);
        }

        var dt = Time.deltaTime / stepsPerUpdate;
        shader.SetFloat(deltaTimeID, dt);

        for (var i = 0; i < stepsPerUpdate; i++)
        {
            shader.Dispatch(kernelGenerateParticleValues, groupsPerRigidBody, 1, 1);
            if (useGrid)
            {
                shader.Dispatch(kernelClearGrid, groupsPerGridCell, 1, 1);
                shader.Dispatch(kernelPopulateGrid, groupsPerParticle, 1, 1);
                shader.Dispatch(kernelCollisionDetectionWithGrid, groupsPerParticle, 1, 1);
            }
            else
            {
                shader.Dispatch(kernelCollisionDetection, groupsPerParticle, 1, 1);
            }

            shader.Dispatch(kernelComputeMomenta, groupsPerRigidBody, 1, 1);
            shader.Dispatch(kernelComputePositionAndRotation, groupsPerRigidBody, 1, 1);
        }

        if (debugWireframe)
        {
            Graphics.DrawMeshInstancedIndirect(sphereMesh, 0, sphereMaterial, bounds, argsSphereBuffer);
            Graphics.DrawMeshInstancedIndirect(lineMesh, 0, lineMaterial, bounds, argsLineBuffer);
        }
        else
        {
            Graphics.DrawMeshInstancedIndirect(mesh, 0, rigidBodyMaterial, bounds, argsBuffer);
        }
    }

    void OnDestroy()
    {
        rigidBodiesBuffer.Release();
        particlesBuffer.Release();

        voxelGridBuffer.Release();

        if (argsSphereBuffer != null) argsSphereBuffer.Release();
        if (argsLineBuffer != null) argsLineBuffer.Release();
        if (argsBuffer != null) argsBuffer.Release();
    }

    void InitArrays()
    {
        rigidBodiesArray = new RigidBody[rigidBodyCount];
        particlesArray = new Particle[rigidBodyCount * particlesPerBody];
        voxelGridArray = new int[gridSize.x * gridSize.y * gridSize.z * 8];
    }

    void InitRigidBodies()
    {
        var pIndex = 0;

        for (var i = 0; i < rigidBodyCount; i++)
        {
            var pos = Random.insideUnitSphere * 5.0f;
            pos.y += 15;
            rigidBodiesArray[i] = new RigidBody(pos, pIndex, particlesPerBody);
            pIndex += particlesPerBody;
        }
    }

    void InitParticles()
    {
        var count = rigidBodyCount * particlesPerBody;

        particlesArray = new Particle[count];

        for (var i = 0; i < rigidBodyCount; i++)
        {
            var body = rigidBodiesArray[i];

            for (var j = 0; j < particleInitialPositions.Count; j++)
            {
                var pos = particleInitialPositions[j];
                particlesArray[body.particleIndex + j] = new Particle(pos);
            }
        }

        Debug.Log("particleCount: " + rigidBodyCount * particlesPerBody);
    }

    void InitBuffers()
    {
        rigidBodiesBuffer = new ComputeBuffer(rigidBodyCount, SIZE_RIGIDBODY);
        rigidBodiesBuffer.SetData(rigidBodiesArray);

        var numOfParticles = rigidBodyCount * particlesPerBody;
        particlesBuffer = new ComputeBuffer(numOfParticles, SIZE_PARTICLE);
        particlesBuffer.SetData(particlesArray);

        var numGridCells = gridSize.x * gridSize.y * gridSize.z;
        voxelGridBuffer = new ComputeBuffer(numGridCells, 8 * sizeof(int));
    }

    void InitShader()
    {
        deltaTimeID = Shader.PropertyToID("deltaTime");

        int[] gridDimensions = { gridSize.x, gridSize.y, gridSize.z };
        shader.SetInts("gridDimensions", gridDimensions);
        shader.SetInt("gridMax", gridSize.x * gridSize.y * gridSize.z);
        shader.SetInt("particlesPerRigidBody", particlesPerBody);
        shader.SetFloat("particleDiameter", particleDiameter);
        shader.SetFloat("springCoefficient", springCoefficient);
        shader.SetFloat("dampingCoefficient", dampingCoefficient);
        shader.SetFloat("frictionCoefficient", frictionCoefficient);
        shader.SetFloat("angularFrictionCoefficient", angularFrictionCoefficient);
        shader.SetFloat("gravityCoefficient", gravityCoefficient);
        shader.SetFloat("tangentialCoefficient", tangentialCoefficient);
        shader.SetFloat("angularForceScalar", angularForceScalar);
        shader.SetFloat("linearForceScalar", linearForceScalar);
        shader.SetFloat("particleMass", cubeMass / particlesPerBody);
        shader.SetInt("particleCount", rigidBodyCount * particlesPerBody);
        var halfSize = new Vector3(gridSize.x, gridSize.y, gridSize.z) * particleDiameter * 0.5f;
        var pos = gridPosition - halfSize;
        shader.SetFloats("gridStartPosition", pos.x, pos.y, pos.z);

        var particleCount = rigidBodyCount * particlesPerBody;
        // Get Kernels
        kernelGenerateParticleValues = shader.FindKernel("GenerateParticleValues");
        kernelClearGrid = shader.FindKernel("ClearGrid");
        kernelPopulateGrid = shader.FindKernel("PopulateGrid");
        kernelCollisionDetectionWithGrid = shader.FindKernel("CollisionDetectionWithGrid");
        kernelComputeMomenta = shader.FindKernel("ComputeMomenta");
        kernelComputePositionAndRotation = shader.FindKernel("ComputePositionAndRotation");
        kernelCollisionDetection = shader.FindKernel("CollisionDetection");

        // Count Thread Groups
        groupsPerRigidBody = Mathf.CeilToInt(rigidBodyCount / 8.0f);
        groupsPerParticle = Mathf.CeilToInt(particleCount / 8f);
        groupsPerGridCell = Mathf.CeilToInt(gridSize.x * gridSize.y * gridSize.z / 8f);

        // Bind buffers

        // kernel 0 GenerateParticleValues
        shader.SetBuffer(kernelGenerateParticleValues, "rigidBodiesBuffer", rigidBodiesBuffer);
        shader.SetBuffer(kernelGenerateParticleValues, "particlesBuffer", particlesBuffer);

        // kernel 1 ClearGrid
        shader.SetBuffer(kernelClearGrid, "voxelGridBuffer", voxelGridBuffer);

        // kernel 2 Populate Grid
        shader.SetBuffer(kernelPopulateGrid, "voxelGridBuffer", voxelGridBuffer);
        shader.SetBuffer(kernelPopulateGrid, "particlesBuffer", particlesBuffer);

        // kernel 3 Collision Detection using Grid
        shader.SetBuffer(kernelCollisionDetectionWithGrid, "particlesBuffer", particlesBuffer);
        shader.SetBuffer(kernelCollisionDetectionWithGrid, "voxelGridBuffer", voxelGridBuffer);

        // kernel 4 Computation of Momenta
        shader.SetBuffer(kernelComputeMomenta, "rigidBodiesBuffer", rigidBodiesBuffer);
        shader.SetBuffer(kernelComputeMomenta, "particlesBuffer", particlesBuffer);

        // kernel 5 Compute Position and Rotation
        shader.SetBuffer(kernelComputePositionAndRotation, "rigidBodiesBuffer", rigidBodiesBuffer);

        // kernel 6 Collision Detection
        shader.SetBuffer(kernelCollisionDetection, "particlesBuffer", particlesBuffer);
        shader.SetBuffer(kernelCollisionDetection, "voxelGridBuffer", voxelGridBuffer);
    }

    void InitInstancing()
    {
        // Setup Indirect Renderer
        rigidBodyMaterial.SetBuffer("rigidBodiesBuffer", rigidBodiesBuffer);

        uint[] args = { vertexCount, 1, 0, 0, 0 };
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        if (debugWireframe)
        {
            var numOfParticles = rigidBodyCount * particlesPerBody;

            uint[] sphereArgs = { sphereMesh.GetIndexCount(0), (uint)numOfParticles, 0, 0, 0 };
            argsSphereBuffer =
                new ComputeBuffer(1, sphereArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsSphereBuffer.SetData(sphereArgs);

            uint[] lineArgs = { lineMesh.GetIndexCount(0), (uint)numOfParticles, 0, 0, 0 };
            argsLineBuffer = new ComputeBuffer(1, lineArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsLineBuffer.SetData(lineArgs);

            sphereMaterial.SetBuffer("particlesBuffer", particlesBuffer);
            sphereMaterial.SetFloat("scale", particleDiameter * 0.5f);

            lineMaterial.SetBuffer("particlesBuffer", particlesBuffer);
        }
    }

    struct RigidBody
    {
        public Vector3 position;
        public Quaternion quaternion;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public readonly int particleIndex;
        public int particleCount;

        public RigidBody(Vector3 pos, int pIndex, int pCount)
        {
            position = pos;
            quaternion = Random.rotation; //Quaternion.identity;
            velocity = angularVelocity = Vector3.zero;
            particleIndex = pIndex;
            particleCount = pCount;
        }
    }

    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public Vector3 localPosition;
        public Vector3 offsetPosition;

        public Particle(Vector3 pos)
        {
            position = velocity = force = offsetPosition = Vector3.zero;
            localPosition = pos;
        }
    }
}