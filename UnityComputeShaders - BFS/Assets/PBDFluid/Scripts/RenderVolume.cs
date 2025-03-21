﻿using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PBDFluid
{
    public class RenderVolume : IDisposable
    {
        const int THREADS = 8;

        public Bounds Bounds;

        GameObject m_mesh;

        readonly ComputeShader m_shader;

        public RenderVolume(Bounds bounds, float pixelSize)
        {
            PixelSize = pixelSize;

            Vector3 min, max;
            min.x = bounds.min.x;
            min.y = bounds.min.y;
            min.z = bounds.min.z;

            max.x = min.x + (float)Math.Ceiling(bounds.size.x / PixelSize);
            max.y = min.y + (float)Math.Ceiling(bounds.size.y / PixelSize);
            max.z = min.z + (float)Math.Ceiling(bounds.size.z / PixelSize);

            Bounds = new Bounds();
            Bounds.SetMinMax(min, max);

            var width = (int)Bounds.size.x;
            var height = (int)Bounds.size.y;
            var depth = (int)Bounds.size.z;

            var groupsX = width / THREADS;
            if (width % THREADS != 0) groupsX++;

            var groupsY = height / THREADS;
            if (height % THREADS != 0) groupsY++;

            var groupsZ = depth / THREADS;
            if (depth % THREADS != 0) groupsZ++;

            Groups = new Vector3Int(groupsX, groupsY, groupsZ);

            Volume = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            Volume.dimension = TextureDimension.Tex3D;
            Volume.volumeDepth = depth;
            Volume.useMipMap = false;
            Volume.enableRandomWrite = true;
            Volume.wrapMode = TextureWrapMode.Clamp;
            Volume.filterMode = FilterMode.Bilinear;
            Volume.Create();

            m_shader = Resources.Load("ComputeVolume") as ComputeShader;
        }

        public float PixelSize { get; }

        public Vector3Int Groups { get; }

        public RenderTexture Volume { get; }

        public bool Hide
        {
            get => m_mesh.activeInHierarchy;
            set => m_mesh.SetActive(!value);
        }

        public Bounds WorldBounds
        {
            get
            {
                var min = Bounds.min;
                var max = min + Bounds.size * PixelSize;

                var bounds = new Bounds();
                bounds.SetMinMax(min, max);

                return bounds;
            }
        }

        public void Dispose()
        {
            if (m_mesh != null)
            {
                Object.DestroyImmediate(m_mesh);
                m_mesh = null;
            }
        }

        /// <summary>
        ///     A inverted cube (material culls front) needs
        ///     to be draw for the ray tracing of the volume.
        /// </summary>
        public void CreateMesh(Material material)
        {
            m_mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_mesh.GetComponent<MeshRenderer>().sharedMaterial = material;

            var bounds = WorldBounds;
            m_mesh.transform.position = bounds.center;
            m_mesh.transform.localScale = bounds.size;

            material.SetVector("Translate", m_mesh.transform.position);
            material.SetVector("Scale", m_mesh.transform.localScale);
            material.SetTexture("Volume", Volume);
            material.SetVector("Size", Bounds.size);
        }

        /// <summary>
        ///     Fills the Volume texture with the particles densities.
        ///     That texture can then be used to render the fluid by
        ///     ray tracing in the meshes shader.
        /// </summary>
        public void FillVolume(FluidBody body, GridHash grid, SmoothingKernel kernel)
        {
            var computeKernel = m_shader.FindKernel("ComputeVolume");

            m_shader.SetFloat("VolumeScale", PixelSize);
            m_shader.SetVector("VolumeSize", Bounds.size);
            m_shader.SetVector("VolumeTranslate", Bounds.min);
            m_shader.SetFloat("HashScale", grid.InvCellSize);
            m_shader.SetVector("HashSize", grid.Bounds.size);
            m_shader.SetVector("HashTranslate", grid.Bounds.min);
            m_shader.SetFloat("KernelRadius", kernel.Radius);
            m_shader.SetFloat("KernelRadius2", kernel.Radius2);
            m_shader.SetFloat("Poly6", kernel.POLY6);
            m_shader.SetFloat("Density", body.Density);
            m_shader.SetInt("NumParticles", body.NumParticles);
            m_shader.SetFloat("ParticleVolume", body.ParticleVolume);
            m_shader.SetBuffer(computeKernel, "IndexMap", grid.IndexMap);
            m_shader.SetBuffer(computeKernel, "Table", grid.Table);
            m_shader.SetBuffer(computeKernel, "Positions", body.Positions);
            m_shader.SetBuffer(computeKernel, "Densities", body.Densities);
            m_shader.SetTexture(computeKernel, "Volume", Volume);

            m_shader.Dispatch(computeKernel, Groups.x, Groups.y, Groups.z);
        }
    }
}