using ProceduralMeshes.Streams;
using UnityEngine;

namespace ProceduralMeshes.Generators
{
    public interface IMeshGenerator
    {
        public int VertexCount { get; }
        public int IndexCount { get; }
        public int JobLength { get; }
        public Bounds Bounds { get; }
        public int Resolution { get; set; }

        public void Execute<S>(int i, S streams) where S : struct, IMeshStreams;
    }
}