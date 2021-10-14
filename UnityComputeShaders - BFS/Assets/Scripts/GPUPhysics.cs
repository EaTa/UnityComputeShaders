using CjLib;
using UnityEngine;

public class GPUPhysics : MonoBehaviour
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

    int activeCount = 0;
    uint[] argsArray;
    ComputeBuffer argsBuffer;

    // calculated
    Vector3 cubeScale;

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

    int particlesPerBody;

    RigidBody[] rigidBodiesArray;

    ComputeBuffer rigidBodiesBuffer;

    int SIZE_PARTICLE = 15 * sizeof(float);

    int SIZE_RIGIDBODY = 13 * sizeof(float) + 2 * sizeof(int);
    ComputeBuffer voxelGridBuffer;

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
    }

    void InitRigidBodies()
    {
    }

    void InitParticles()
    {
    }

    void InitBuffers()
    {
    }

    void InitShader()
    {
    }

    void InitInstancing()
    {
    }

    struct RigidBody
    {
        public Vector3 position;
        public Quaternion quaternion;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public int particleIndex;
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