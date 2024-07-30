using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dicer.Generators
{
    public static class GenerationUtils
    {
        public static Vector3 GeoCenter(Transform transform, Mesh mesh, IEnumerable<int> indices)
        {
            Vector3 sum = Vector3.zero;
            foreach (int index in indices)
            {
                sum += mesh.vertices[index];
            }

            return sum / indices.Count();
        }
    }
}
