using System;
using UnityEngine;

namespace PBDFluid
{
    public class GridHash : IDisposable
    {
        const int THREADS = 128;
        const int READ = 0;
        const int WRITE = 1;

        public Bounds Bounds;

        readonly int m_hashKernel;
        readonly int m_clearKernel;
        readonly int m_mapKernel;

        readonly ComputeShader m_shader;

        readonly BitonicSort m_sort;

        public GridHash(Bounds bounds, int numParticles, float cellSize)
        {
            TotalParticles = numParticles;
            CellSize = cellSize;
            InvCellSize = 1.0f / CellSize;

            Groups = TotalParticles / THREADS;
            if (TotalParticles % THREADS != 0) Groups++;

            Vector3 min, max;
            min = bounds.min;

            max.x = min.x + (float)Math.Ceiling(bounds.size.x / CellSize);
            max.y = min.y + (float)Math.Ceiling(bounds.size.y / CellSize);
            max.z = min.z + (float)Math.Ceiling(bounds.size.z / CellSize);

            Bounds = new Bounds();
            Bounds.SetMinMax(min, max);

            var width = (int)Bounds.size.x;
            var height = (int)Bounds.size.y;
            var depth = (int)Bounds.size.z;

            var size = width * height * depth;

            IndexMap = new ComputeBuffer(TotalParticles, 2 * sizeof(int));
            Table = new ComputeBuffer(size, 2 * sizeof(int));

            m_sort = new BitonicSort(TotalParticles);

            m_shader = Resources.Load("GridHash") as ComputeShader;
            m_hashKernel = m_shader.FindKernel("HashParticles");
            m_clearKernel = m_shader.FindKernel("ClearTable");
            m_mapKernel = m_shader.FindKernel("MapTable");
        }

        public int TotalParticles { get; }

        public float CellSize { get; }

        public float InvCellSize { get; }

        public int Groups { get; }

        /// <summary>
        ///     Contains the particles hash value (x) and
        ///     the particles index in its position array (y)
        /// </summary>
        public ComputeBuffer IndexMap { get; private set; }

        /// <summary>
        ///     Is a 3D grid representing the hash cells.
        ///     Contans where the sorted hash values start (x)
        ///     and end (y) for each cell.
        /// </summary>
        public ComputeBuffer Table { get; private set; }

        public Bounds WorldBounds
        {
            get
            {
                var min = Bounds.min;
                var max = min + Bounds.size * CellSize;

                var bounds = new Bounds();
                bounds.SetMinMax(min, max);

                return bounds;
            }
        }

        public void Dispose()
        {
            m_sort.Dispose();

            if (IndexMap != null)
            {
                IndexMap.Release();
                IndexMap = null;
            }

            if (Table != null)
            {
                Table.Release();
                Table = null;
            }
        }

        public void Process(ComputeBuffer particles)
        {
            if (particles.count != TotalParticles)
                throw new ArgumentException("particles.Length != TotalParticles");

            m_shader.SetInt("NumParticles", TotalParticles);
            m_shader.SetInt("TotalParticles", TotalParticles);
            m_shader.SetFloat("HashScale", InvCellSize);
            m_shader.SetVector("HashSize", Bounds.size);
            m_shader.SetVector("HashTranslate", Bounds.min);

            m_shader.SetBuffer(m_hashKernel, "Particles", particles);
            m_shader.SetBuffer(m_hashKernel, "Boundary",
                particles); //unity 2018 complains if boundary not set in kernel
            m_shader.SetBuffer(m_hashKernel, "IndexMap", IndexMap);

            //Assign the particles hash to x and index to y.
            m_shader.Dispatch(m_hashKernel, Groups, 1, 1);

            MapTable();
        }

        public void Process(ComputeBuffer particles, ComputeBuffer boundary)
        {
            var numParticles = particles.count;
            var numBoundary = boundary.count;

            if (numParticles + numBoundary != TotalParticles)
                throw new ArgumentException("numParticles + numBoundary != TotalParticles");

            m_shader.SetInt("NumParticles", numParticles);
            m_shader.SetInt("TotalParticles", TotalParticles);
            m_shader.SetFloat("HashScale", InvCellSize);
            m_shader.SetVector("HashSize", Bounds.size);
            m_shader.SetVector("HashTranslate", Bounds.min);

            m_shader.SetBuffer(m_hashKernel, "Particles", particles);
            m_shader.SetBuffer(m_hashKernel, "Boundary", boundary);
            m_shader.SetBuffer(m_hashKernel, "IndexMap", IndexMap);

            //Assign the particles hash to x and index to y.
            m_shader.Dispatch(m_hashKernel, Groups, 1, 1);

            MapTable();
        }

        void MapTable()
        {
            //First sort by the hash values in x.
            //Uses bitonic sort but any other method will work.
            m_sort.Sort(IndexMap);

            m_shader.SetInt("TotalParticles", TotalParticles);
            m_shader.SetBuffer(m_clearKernel, "Table", Table);

            //Clear the previous tables values as not all
            //locations will be written to when mapped.
            m_shader.Dispatch(m_clearKernel, Groups, 1, 1);

            m_shader.SetBuffer(m_mapKernel, "IndexMap", IndexMap);
            m_shader.SetBuffer(m_mapKernel, "Table", Table);

            //For each hash cell find where the index map
            //start and ends for that hash value.
            m_shader.Dispatch(m_mapKernel, Groups, 1, 1);
        }

        /// <summary>
        ///     Draws the hash grid for debugging.
        /// </summary>
        public void DrawGrid(Camera cam, Color col)
        {
            var width = Bounds.size.x;
            var height = Bounds.size.y;
            var depth = Bounds.size.z;

            DrawLines.LineMode = LINE_MODE.LINES;

            for (float y = 0; y <= height; y++)
            {
                for (float x = 0; x <= width; x++)
                {
                    var a = Bounds.min + new Vector3(x, y, 0) * CellSize;
                    var b = Bounds.min + new Vector3(x, y, depth) * CellSize;

                    DrawLines.Draw(cam, a, b, col, Matrix4x4.identity);
                }

                for (float z = 0; z <= depth; z++)
                {
                    var a = Bounds.min + new Vector3(0, y, z) * CellSize;
                    var b = Bounds.min + new Vector3(width, y, z) * CellSize;

                    DrawLines.Draw(cam, a, b, col, Matrix4x4.identity);
                }
            }

            for (float z = 0; z <= depth; z++)
            for (float x = 0; x <= width; x++)
            {
                var a = Bounds.min + new Vector3(x, 0, z) * CellSize;
                var b = Bounds.min + new Vector3(x, height, z) * CellSize;

                DrawLines.Draw(cam, a, b, col, Matrix4x4.identity);
            }
        }
    }
}