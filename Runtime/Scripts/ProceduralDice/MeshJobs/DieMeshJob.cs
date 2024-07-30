using Dicer.Generators;
using ProceduralMeshes.Generators;
using ProceduralMeshes.Streams;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Dicer
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct DieMeshJob<G, S> : IJobFor
        where G : struct, IDiceGenerator
        where S : struct, IMeshStreams
    {
        private G _generator;
        [WriteOnly]
        private S _streams;

        public void Execute(int i) => _generator.Execute(i, _streams);

        public static JobHandle ScheduleParallel(
            Mesh mesh, Mesh.MeshData meshData, int resolution, int dieSize, JobHandle dependency
        )
        {
            var job = new DieMeshJob<G, S>();
            job._generator.Resolution = resolution;
            job._generator.DieSize = dieSize;

            job._streams.Setup(
                meshData,
                mesh.bounds = job._generator.Bounds,
                job._generator.VertexCount,
                job._generator.IndexCount
            );

            return job.ScheduleParallel(job._generator.JobLength, 1, dependency);
        }
    }

    public delegate JobHandle DieMeshJobScheduleDelegate(
        Mesh mesh, Mesh.MeshData meshData, int resolution, int dieSize, JobHandle dependency
    );
}
