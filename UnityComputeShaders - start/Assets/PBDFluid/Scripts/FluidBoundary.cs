using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace PBDFluid
{
    public class FluidBoundary : IDisposable
    {
        const int THREADS = 128;

        public Bounds Bounds;

        ComputeBuffer m_argsBuffer;

        public FluidBoundary(ParticleSource source, float radius, float density, Matrix4x4 RTS)
        {
            NumParticles = source.NumParticles;
            ParticleRadius = radius;
            Density = density;

            CreateParticles(source, RTS);
            CreateBoundryPsi();
        }

        public int NumParticles { get; }

        public float ParticleRadius { get; }

        public float ParticleDiameter => ParticleRadius * 2.0f;

        public float Density { get; }

        public ComputeBuffer Positions { get; private set; }

        public void Dispose()
        {
            if (Positions != null)
            {
                Positions.Release();
                Positions = null;
            }

            CBUtility.Release(ref m_argsBuffer);
        }

        /// <summary>
        ///     Draws the mesh spheres when draw particles is enabled.
        /// </summary>
        public void Draw(Camera cam, Mesh mesh, Material material, int layer)
        {
            if (m_argsBuffer == null)
                CreateArgBuffer(mesh.GetIndexCount(0));

            material.SetBuffer("positions", Positions);
            material.SetColor("color", Color.red);
            material.SetFloat("diameter", ParticleDiameter);

            var castShadow = ShadowCastingMode.Off;
            var recieveShadow = false;

            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, Bounds, m_argsBuffer, 0, null, castShadow,
                recieveShadow, layer, cam);
        }

        void CreateParticles(ParticleSource source, Matrix4x4 RTS)
        {
            var positions = new Vector4[NumParticles];

            var inf = float.PositiveInfinity;
            var min = new Vector3(inf, inf, inf);
            var max = new Vector3(-inf, -inf, -inf);

            for (var i = 0; i < NumParticles; i++)
            {
                var pos = RTS * source.Positions[i];
                positions[i] = pos;

                if (pos.x < min.x) min.x = pos.x;
                if (pos.y < min.y) min.y = pos.y;
                if (pos.z < min.z) min.z = pos.z;

                if (pos.x > max.x) max.x = pos.x;
                if (pos.y > max.y) max.y = pos.y;
                if (pos.z > max.z) max.z = pos.z;
            }

            min.x -= ParticleRadius;
            min.y -= ParticleRadius;
            min.z -= ParticleRadius;

            max.x += ParticleRadius;
            max.y += ParticleRadius;
            max.z += ParticleRadius;

            Bounds = new Bounds();
            Bounds.SetMinMax(min, max);

            Positions = new ComputeBuffer(NumParticles, 4 * sizeof(float));
            Positions.SetData(positions);
        }

        void CreateArgBuffer(uint indexCount)
        {
            var args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = indexCount;
            args[1] = (uint)NumParticles;

            m_argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            m_argsBuffer.SetData(args);
        }

        void CreateBoundryPsi()
        {
            var cellSize = ParticleRadius * 4.0f;
            var K = new SmoothingKernel(cellSize);

            var grid = new GridHash(Bounds, NumParticles, cellSize);
            grid.Process(Positions);

            var shader = Resources.Load("FluidBoundary") as ComputeShader;

            var kernel = shader.FindKernel("ComputePsi");

            shader.SetFloat("Density", Density);
            shader.SetFloat("KernelRadiuse", K.Radius);
            shader.SetFloat("KernelRadius2", K.Radius2);
            shader.SetFloat("Poly6", K.POLY6);
            shader.SetFloat("Poly6Zero", K.Poly6(Vector3.zero));
            shader.SetInt("NumParticles", NumParticles);

            shader.SetFloat("HashScale", grid.InvCellSize);
            shader.SetVector("HashSize", grid.Bounds.size);
            shader.SetVector("HashTranslate", grid.Bounds.min);
            shader.SetBuffer(kernel, "IndexMap", grid.IndexMap);
            shader.SetBuffer(kernel, "Table", grid.Table);

            shader.SetBuffer(kernel, "Boundary", Positions);

            var groups = NumParticles / THREADS;
            if (NumParticles % THREADS != 0) groups++;

            //Fills the boundarys psi array so the fluid can
            //collide against it smoothly. The original computes
            //the phi for each boundary particle based on the
            //density of the boundary but I find the fluid 
            //leaks out so Im just using a const value.

            shader.Dispatch(kernel, groups, 1, 1);

            grid.Dispose();
        }
    }
}