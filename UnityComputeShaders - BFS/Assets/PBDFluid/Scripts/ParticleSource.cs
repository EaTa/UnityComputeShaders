using System.Collections.Generic;
using UnityEngine;

namespace PBDFluid
{
    public abstract class ParticleSource
    {
        public ParticleSource(float spacing)
        {
            Spacing = spacing;
        }

        public int NumParticles => Positions.Count;

        public IList<Vector3> Positions { get; protected set; }

        public float Spacing { get; }

        public float HalfSpacing => Spacing * 0.5f;
    }
}