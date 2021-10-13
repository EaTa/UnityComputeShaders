﻿using System;
using UnityEngine;

namespace PBDFluid
{
    public class FluidSolver : IDisposable
    {
        const int THREADS = 128;
        const int READ = 0;
        const int WRITE = 1;

        readonly ComputeShader m_shader;

        public FluidSolver(FluidBody body, FluidBoundary boundary)
        {
            SolverIterations = 2;
            ConstraintIterations = 2;

            Body = body;
            Boundary = boundary;

            var cellSize = Body.ParticleRadius * 4.0f;
            var total = Body.NumParticles + Boundary.NumParticles;
            Hash = new GridHash(Boundary.Bounds, total, cellSize);
            Kernel = new SmoothingKernel(cellSize);

            var numParticles = Body.NumParticles;
            Groups = numParticles / THREADS;
            if (numParticles % THREADS != 0) Groups++;

            m_shader = Resources.Load("FluidSolver") as ComputeShader;
        }

        public int Groups { get; }

        public FluidBoundary Boundary { get; }

        public FluidBody Body { get; }

        public GridHash Hash { get; }

        public int SolverIterations { get; set; }

        public int ConstraintIterations { get; set; }

        public SmoothingKernel Kernel { get; }

        public void Dispose()
        {
            Hash.Dispose();
        }

        public void StepPhysics(float dt)
        {
            if (dt <= 0.0) return;
            if (SolverIterations <= 0 || ConstraintIterations <= 0) return;

            dt /= SolverIterations;

            m_shader.SetInt("NumParticles", Body.NumParticles);
            m_shader.SetVector("Gravity", new Vector3(0.0f, -9.81f, 0.0f));
            m_shader.SetFloat("Dampning", Body.Dampning);
            m_shader.SetFloat("DeltaTime", dt);
            m_shader.SetFloat("Density", Body.Density);
            m_shader.SetFloat("Viscosity", Body.Viscosity);
            m_shader.SetFloat("ParticleMass", Body.ParticleMass);

            m_shader.SetFloat("KernelRadius", Kernel.Radius);
            m_shader.SetFloat("KernelRadius2", Kernel.Radius2);
            m_shader.SetFloat("Poly6Zero", Kernel.Poly6(Vector3.zero));
            m_shader.SetFloat("Poly6", Kernel.POLY6);
            m_shader.SetFloat("SpikyGrad", Kernel.SPIKY_GRAD);
            m_shader.SetFloat("ViscLap", Kernel.VISC_LAP);

            m_shader.SetFloat("HashScale", Hash.InvCellSize);
            m_shader.SetVector("HashSize", Hash.Bounds.size);
            m_shader.SetVector("HashTranslate", Hash.Bounds.min);

            //Predicted and velocities use a double buffer as solver step
            //needs to read from many locations of buffer and write the result
            //in same pass. Could be removed if needed as long as buffer writes 
            //are atomic. Not sure if they are.

            for (var i = 0; i < SolverIterations; i++)
            {
                PredictPositions(dt);

                Hash.Process(Body.Predicted[READ], Boundary.Positions);

                ConstrainPositions();

                UpdateVelocities(dt);

                SolveViscosity();

                UpdatePositions();
            }
        }

        void PredictPositions(float dt)
        {
            var kernel = m_shader.FindKernel("PredictPositions");

            m_shader.SetBuffer(kernel, "Positions", Body.Positions);
            m_shader.SetBuffer(kernel, "PredictedWRITE", Body.Predicted[WRITE]);
            m_shader.SetBuffer(kernel, "VelocitiesREAD", Body.Velocities[READ]);
            m_shader.SetBuffer(kernel, "VelocitiesWRITE", Body.Velocities[WRITE]);

            m_shader.Dispatch(kernel, Groups, 1, 1);

            Swap(Body.Predicted);
            Swap(Body.Velocities);
        }

        public void ConstrainPositions()
        {
            var computeKernel = m_shader.FindKernel("ComputeDensity");
            var solveKernel = m_shader.FindKernel("SolveConstraint");

            m_shader.SetBuffer(computeKernel, "Densities", Body.Densities);
            m_shader.SetBuffer(computeKernel, "Pressures", Body.Pressures);
            m_shader.SetBuffer(computeKernel, "Boundary", Boundary.Positions);
            m_shader.SetBuffer(computeKernel, "IndexMap", Hash.IndexMap);
            m_shader.SetBuffer(computeKernel, "Table", Hash.Table);

            m_shader.SetBuffer(solveKernel, "Pressures", Body.Pressures);
            m_shader.SetBuffer(solveKernel, "Boundary", Boundary.Positions);
            m_shader.SetBuffer(solveKernel, "IndexMap", Hash.IndexMap);
            m_shader.SetBuffer(solveKernel, "Table", Hash.Table);

            for (var i = 0; i < ConstraintIterations; i++)
            {
                m_shader.SetBuffer(computeKernel, "PredictedREAD", Body.Predicted[READ]);
                m_shader.Dispatch(computeKernel, Groups, 1, 1);

                m_shader.SetBuffer(solveKernel, "PredictedREAD", Body.Predicted[READ]);
                m_shader.SetBuffer(solveKernel, "PredictedWRITE", Body.Predicted[WRITE]);
                m_shader.Dispatch(solveKernel, Groups, 1, 1);

                Swap(Body.Predicted);
            }
        }

        void UpdateVelocities(float dt)
        {
            var kernel = m_shader.FindKernel("UpdateVelocities");

            m_shader.SetBuffer(kernel, "Positions", Body.Positions);
            m_shader.SetBuffer(kernel, "PredictedREAD", Body.Predicted[READ]);
            m_shader.SetBuffer(kernel, "VelocitiesWRITE", Body.Velocities[WRITE]);

            m_shader.Dispatch(kernel, Groups, 1, 1);

            Swap(Body.Velocities);
        }

        void SolveViscosity()
        {
            var kernel = m_shader.FindKernel("SolveViscosity");

            m_shader.SetBuffer(kernel, "Densities", Body.Densities);
            m_shader.SetBuffer(kernel, "Boundary", Boundary.Positions);
            m_shader.SetBuffer(kernel, "IndexMap", Hash.IndexMap);
            m_shader.SetBuffer(kernel, "Table", Hash.Table);

            m_shader.SetBuffer(kernel, "PredictedREAD", Body.Predicted[READ]);
            m_shader.SetBuffer(kernel, "VelocitiesREAD", Body.Velocities[READ]);
            m_shader.SetBuffer(kernel, "VelocitiesWRITE", Body.Velocities[WRITE]);

            m_shader.Dispatch(kernel, Groups, 1, 1);

            Swap(Body.Velocities);
        }

        void UpdatePositions()
        {
            var kernel = m_shader.FindKernel("UpdatePositions");

            m_shader.SetBuffer(kernel, "Positions", Body.Positions);
            m_shader.SetBuffer(kernel, "PredictedREAD", Body.Predicted[READ]);

            m_shader.Dispatch(kernel, Groups, 1, 1);
        }

        void Swap(ComputeBuffer[] buffers)
        {
            var tmp = buffers[0];
            buffers[0] = buffers[1];
            buffers[1] = tmp;
        }
    }
}