using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace PBDFluid
{
    public class FluidBody : IDisposable
    {
        public Bounds Bounds;

        ComputeBuffer m_argsBuffer;

        public FluidBody(ParticleSource source, float radius, float density, Matrix4x4 RTS)
        {
            NumParticles = source.NumParticles;
            Density = density;
            Viscosity = 0.002f;
            Dampning = 0.0f;

            ParticleRadius = radius;
            ParticleVolume = 4.0f / 3.0f * Mathf.PI * Mathf.Pow(radius, 3);
            ParticleMass = ParticleVolume * Density;

            Densities = new ComputeBuffer(NumParticles, sizeof(float));
            Pressures = new ComputeBuffer(NumParticles, sizeof(float));

            CreateParticles(source, RTS);
        }

        public int NumParticles { get; }

        public float Density { get; set; }

        public float Viscosity { get; set; }

        public float Dampning { get; set; }

        public float ParticleRadius { get; }

        public float ParticleDiameter => ParticleRadius * 2.0f;

        public float ParticleMass { get; set; }

        public float ParticleVolume { get; }

        public ComputeBuffer Pressures { get; private set; }

        public ComputeBuffer Densities { get; private set; }

        public ComputeBuffer Positions { get; private set; }

        public ComputeBuffer[] Predicted { get; private set; }

        public ComputeBuffer[] Velocities { get; private set; }

        public void Dispose()
        {
            if (Positions != null)
            {
                Positions.Release();
                Positions = null;
            }

            if (Densities != null)
            {
                Densities.Release();
                Densities = null;
            }

            if (Pressures != null)
            {
                Pressures.Release();
                Pressures = null;
            }

            CBUtility.Release(Predicted);
            CBUtility.Release(Velocities);
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
            material.SetColor("color", Color.white);
            material.SetFloat("diameter", ParticleDiameter);

            var castShadow = ShadowCastingMode.On;
            var recieveShadow = true;

            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, Bounds, m_argsBuffer, 0, null, castShadow,
                recieveShadow, layer, cam);
        }

        void CreateParticles(ParticleSource source, Matrix4x4 RTS)
        {
            var positions = new Vector4[NumParticles];
            var predicted = new Vector4[NumParticles];
            var velocities = new Vector4[NumParticles];

            var inf = float.PositiveInfinity;
            var min = new Vector3(inf, inf, inf);
            var max = new Vector3(-inf, -inf, -inf);

            for (var i = 0; i < NumParticles; i++)
            {
                var pos = RTS * source.Positions[i];
                positions[i] = pos;
                predicted[i] = pos;

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

            //Predicted and velocities use a double buffer as solver step
            //needs to read from many locations of buffer and write the result
            //in same pass. Could be removed if needed as long as buffer writes 
            //are atomic. Not sure if they are.

            Predicted = new ComputeBuffer[2];
            Predicted[0] = new ComputeBuffer(NumParticles, 4 * sizeof(float));
            Predicted[0].SetData(predicted);
            Predicted[1] = new ComputeBuffer(NumParticles, 4 * sizeof(float));
            Predicted[1].SetData(predicted);

            Velocities = new ComputeBuffer[2];
            Velocities[0] = new ComputeBuffer(NumParticles, 4 * sizeof(float));
            Velocities[0].SetData(velocities);
            Velocities[1] = new ComputeBuffer(NumParticles, 4 * sizeof(float));
            Velocities[1].SetData(velocities);
        }

        void CreateArgBuffer(uint indexCount)
        {
            var args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = indexCount;
            args[1] = (uint)NumParticles;

            m_argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            m_argsBuffer.SetData(args);
        }
    }
}