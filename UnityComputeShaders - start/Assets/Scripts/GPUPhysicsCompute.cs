using CjLib;
using UnityEngine;

public class GPUPhysicsCompute : MonoBehaviour
{
    public ComputeShader shader;
    public Material cubeMaterial;
    public Bounds bounds;
    public float cubeMass;
    public float scale;
    public int particlesPerEdge;
    public float springCoefficient;
    public float dampingCoefficient;
    public float tangentialCoefficient;
    public float gravityCoefficient;
    public float frictionCoefficient;
    public float angularFrictionCoefficient;
    public float angularForceScalar;
    public float linearForceScalar;
    public int rigidBodyCount = 1000;

    [Range(1, 20)] public int stepsPerUpdate = 10;

    int activeCount;
    readonly uint[] argsArray = { 0, 0, 0, 0, 0 };
    ComputeBuffer argsBuffer;
    int deltaTimeID;

    int frameCounter;
    int groupsPerParticle;

    int groupsPerRigidBody;
    int kernelCollisionDetection;
    int kernelComputeMomenta;
    int kernelComputePositionAndRotation;

    int kernelGenerateParticleValues;
    float particleDiameter;
    Particle[] particlesArray;
    ComputeBuffer particlesBuffer;

    // calculated
    int particlesPerBody;

    RigidBody[] rigidBodiesArray;

    ComputeBuffer rigidBodiesBuffer;

    readonly int SIZE_PARTICLE = 15 * sizeof(float);

    readonly int SIZE_RIGIDBODY = 13 * sizeof(float) + 2 * sizeof(int);

    // set from editor
    public Mesh cubeMesh => PrimitiveMeshFactory.BoxFlatShaded();

    void Start()
    {
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
            argsArray[1] = (uint)activeCount;
            argsBuffer.SetData(argsArray);
        }

        var dt = Time.deltaTime / stepsPerUpdate;
        shader.SetFloat(deltaTimeID, dt);

        for (var i = 0; i < stepsPerUpdate; i++)
        {
            shader.Dispatch(kernelGenerateParticleValues, groupsPerRigidBody, 1, 1);
            shader.Dispatch(kernelCollisionDetection, groupsPerParticle, 1, 1);
            shader.Dispatch(kernelComputeMomenta, groupsPerRigidBody, 1, 1);
            shader.Dispatch(kernelComputePositionAndRotation, groupsPerRigidBody, 1, 1);
        }

        Graphics.DrawMeshInstancedIndirect(cubeMesh, 0, cubeMaterial, bounds, argsBuffer);
    }

    void OnDestroy()
    {
        rigidBodiesBuffer.Release();
        particlesBuffer.Release();

        if (argsBuffer != null) argsBuffer.Release();
    }

    void InitArrays()
    {
        particlesPerBody = particlesPerEdge * particlesPerEdge * particlesPerEdge;

        rigidBodiesArray = new RigidBody[rigidBodyCount];
        particlesArray = new Particle[rigidBodyCount * particlesPerBody];
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
        particleDiameter = scale / particlesPerEdge;

        // initial local particle positions within a rigidbody
        var index = 0;
        var centerer = (particleDiameter - scale) * 0.5f;
        var centeringOffset = new Vector3(centerer, centerer, centerer);

        for (var x = 0; x < particlesPerEdge; x++)
        for (var y = 0; y < particlesPerEdge; y++)
        for (var z = 0; z < particlesPerEdge; z++)
        {
            var pos = centeringOffset + new Vector3(x, y, z) * particleDiameter;
            for (var i = 0; i < rigidBodyCount; i++)
            {
                var body = rigidBodiesArray[i];
                particlesArray[body.particleIndex + index] = new Particle(pos);
            }

            index++;
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
    }

    void InitShader()
    {
        deltaTimeID = Shader.PropertyToID("deltaTime");

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
        var particleCount = rigidBodyCount * particlesPerBody;
        shader.SetInt("particleCount", particleCount);

        // Get Kernels
        kernelGenerateParticleValues = shader.FindKernel("GenerateParticleValues");
        kernelCollisionDetection = shader.FindKernel("CollisionDetection");
        kernelComputeMomenta = shader.FindKernel("ComputeMomenta");
        kernelComputePositionAndRotation = shader.FindKernel("ComputePositionAndRotation");

        // Count Thread Groups
        groupsPerRigidBody = Mathf.CeilToInt(rigidBodyCount / 8.0f);
        groupsPerParticle = Mathf.CeilToInt(particleCount / 8f);

        // Bind buffers

        // kernel 0 GenerateParticleValues
        shader.SetBuffer(kernelGenerateParticleValues, "rigidBodiesBuffer", rigidBodiesBuffer);
        shader.SetBuffer(kernelGenerateParticleValues, "particlesBuffer", particlesBuffer);

        // kernel 1 Collision Detection
        shader.SetBuffer(kernelCollisionDetection, "particlesBuffer", particlesBuffer);

        // kernel 2 Computation of Momenta
        shader.SetBuffer(kernelComputeMomenta, "rigidBodiesBuffer", rigidBodiesBuffer);
        shader.SetBuffer(kernelComputeMomenta, "particlesBuffer", particlesBuffer);

        // kernel 3 Compute Position and Rotation
        shader.SetBuffer(kernelComputePositionAndRotation, "rigidBodiesBuffer", rigidBodiesBuffer);
    }

    void InitInstancing()
    {
        // Setup Indirect Renderer
        cubeMaterial.SetBuffer("rigidBodiesBuffer", rigidBodiesBuffer);

        argsArray[0] = cubeMesh.GetIndexCount(0);
        argsBuffer = new ComputeBuffer(1, argsArray.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);
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