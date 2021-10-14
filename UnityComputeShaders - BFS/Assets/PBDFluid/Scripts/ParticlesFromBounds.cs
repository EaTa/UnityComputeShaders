using System.Collections.Generic;
using UnityEngine;

namespace PBDFluid
{
    public class ParticlesFromBounds : ParticleSource
    {
        public ParticlesFromBounds(float spacing, Bounds bounds) : base(spacing)
        {
            Bounds = bounds;
            Exclusion = new List<Bounds>();
            CreateParticles();
        }

        public ParticlesFromBounds(float spacing, Bounds bounds, Bounds exclusion) : base(spacing)
        {
            Bounds = bounds;
            Exclusion = new List<Bounds>();
            Exclusion.Add(exclusion);
            CreateParticles();
        }

        public Bounds Bounds { get; }

        public List<Bounds> Exclusion { get; }

        void CreateParticles()
        {
            var numX = (int)((Bounds.size.x + HalfSpacing) / Spacing);
            var numY = (int)((Bounds.size.y + HalfSpacing) / Spacing);
            var numZ = (int)((Bounds.size.z + HalfSpacing) / Spacing);

            Positions = new List<Vector3>();

            for (var z = 0; z < numZ; z++)
            for (var y = 0; y < numY; y++)
            for (var x = 0; x < numX; x++)
            {
                var pos = new Vector3();
                pos.x = Spacing * x + Bounds.min.x + HalfSpacing;
                pos.y = Spacing * y + Bounds.min.y + HalfSpacing;
                pos.z = Spacing * z + Bounds.min.z + HalfSpacing;

                var exclude = false;
                for (var i = 0; i < Exclusion.Count; i++)
                    if (Exclusion[i].Contains(pos))
                    {
                        exclude = true;
                        break;
                    }

                if (!exclude)
                    Positions.Add(pos);
            }
        }
    }
}