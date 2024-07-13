using ProceduralMeshes.Streams;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace ProceduralMeshes.Generators
{

    public struct PolyhedronDiceGenerator : IMeshGenerator
    {
        // To form all triangles one vertex is added to current mesh, but for the first triangle all 3 vertices must be added   
        public int VertexCount => ActualResolution + 2;
        
        // Each side of polyhedron is a triangle
        public int IndexCount => 3 * ActualResolution;

        // Let's try singlethreaded version at first
        public int JobLength => 1;
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
        public int Resolution { get; set; }

        public int ActualResolution
        {
            get
            {
                if (Resolution < 4 || Resolution % 2 == 0)
                {
                    return Resolution;
                }

                return Resolution * 2;
            }
        }

        public MeshJobScheduleDelegate DefaultJobHandle => MeshJob<PolyhedronDiceGenerator, MultiStream>.ScheduleParallel;

        public void Execute<S>(int i, S streams) where S : struct, IMeshStreams
        {
            if (ActualResolution < 4)
            {
                throw new System.Exception("Polyhedron dice have 4+ sides");
            }

            // Build initial triangle first three vertices

            for (int vi = 4; vi < ActualResolution; vi++)
            {
                // Add vertex of each subsequent triangle
                // And it's indices
            }
        }
    }
}