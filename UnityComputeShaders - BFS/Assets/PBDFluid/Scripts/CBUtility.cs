﻿using System.Collections.Generic;
using UnityEngine;

namespace PBDFluid
{
    public static class CBUtility
    {
        public static void Release(ref ComputeBuffer buffer)
        {
            if (buffer == null) return;
            buffer.Release();
            buffer = null;
        }

        public static void Release(IList<ComputeBuffer> buffers)
        {
            if (buffers == null) return;

            var count = buffers.Count;
            for (var i = 0; i < count; i++)
            {
                if (buffers[i] == null) continue;
                buffers[i].Release();
                buffers[i] = null;
            }
        }

        public static void Swap(ComputeBuffer[] buffers)
        {
            var tmp = buffers[0];
            buffers[0] = buffers[1];
            buffers[1] = tmp;
        }
    }
}