using ProceduralMeshes.Streams;
using ProceduralMeshes.Generators;
using ProceduralMeshes;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Dicer.Constraints;
using Dicer.TextureGeneration;

using static Unity.Mathematics.math;

namespace Dicer.Generators
{
    public struct TetrahedronDiceGenerator : IDiceGenerator
    {
        public int VertexCount => 2 * (Resolution + 1) * (Resolution + 2);
        public int IndexCount => 12 * Resolution * Resolution;
        public int JobLength => ActualDieSize;
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
        public int Resolution { get; set; }

        public int DieSize {  get; set; }

        public int ActualDieSize => DieSize;
        public ISizeConstraint Constraint => new LinearSizeConstraint(0, 4);

        public DieMeshJobScheduleDelegate DefaultDieJobHandle => DieMeshJob<TetrahedronDiceGenerator, MultiStream>.ScheduleParallel;

        private static float3 GetCorner(int index)
        {
            return index switch
            {
                0 => new float3(-sqrt(2f / 9f), sqrt(2f / 3f), -1f / 3f),
                1 => new float3(-sqrt(2f / 9f), -sqrt(2f / 3f), -1f / 3f),
                2 => new float3(0f, 0f, 1f),
                3 => new float3(sqrt(8f / 9f), 0f, -1f / 3f),
                _ => throw new System.Exception($"Tetrahedron has only four corners, indexed from 0 to 3. Received argument: {index}")
            };
        }

        private struct Side
        {
            public float3 Top;
            public float3 Left;
            public float3 Right;
        }

        private struct TexSide
        {
            public float2 Top;
            public float2 Left;
            public float2 Right;
        }

        private static Side GetSide(int side)
        {
            return side switch
            {
                0 => new Side { Top = GetCorner(0), Left = GetCorner(1), Right = GetCorner(2) },
                1 => new Side { Top = GetCorner(0), Left = GetCorner(3), Right = GetCorner(1) },
                2 => new Side { Top = GetCorner(0), Left = GetCorner(2), Right = GetCorner(3) },
                3 => new Side { Top = GetCorner(3), Left = GetCorner(2), Right = GetCorner(1) },
                _ => throw new System.Exception($"Tetrahedron has only four sides, indexed from 0 to 3. Received argument: {side}")
            };
        }

        private static TexSide GetTexSide(int side)
        {
            return side switch
            {
                0 => new TexSide { Top = new float2(0.5f, 1f / 2f + sqrt(3) / 4f), Left = new float2(0.25f, 0.5f), Right = new float2(0.75f, 0.5f) },
                1 => new TexSide { Top = new float2(0.5f, 1f / 2f + sqrt(3) / 4f), Left = new float2(0, 1f / 2f + sqrt(3) / 4f), Right = new float2(0.25f, 0.5f) },
                2 => new TexSide { Top = new float2(0.5f, 1f / 2f + sqrt(3) / 4f), Left = new float2(0.75f, 0.5f), Right = new float2(1, 1f / 2f + sqrt(3) / 4f) },
                3 => new TexSide { Top = new float2(0.5f, 1f / 2f - sqrt(3) / 4f), Left = new float2(0.75f, 0.5f), Right = new float2(0.25f, 0.5f) },
                _ => throw new System.Exception($"Tetrahedron has only four sides, indexed from 0 to 3. Received argument: {side}")
            };
        }

        private static Vector3 GetCornerAngle(int corner)
        {
            return corner switch
            {
                0 => new Vector3(0, 180, -60),
                1 => new Vector3(180, 0, -120),
                2 => new Vector3(180, 0, 0),
                _ => throw new System.Exception($"Triangle has only three corners, indexed from 0 to 2. Received argument: {corner}")
            };
        }

        private static Vector3 GetSideAngle(int side)
        {
            return side switch
            {
                0 => new Vector3(0, 0, 300),
                1 => new Vector3(0, 0, 0),
                2 => new Vector3(0, 0, -120),
                3 => new Vector3(0, 0, 120),
                _ => throw new System.Exception($"Tetrahedron has only four corners, indexed from 0 to 3. Received argument: {side}")
            };
        }

        public Texture2D GenerateSidesTexture(int width, int height, SideTextureRenderer renderer)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            var temporaryTexture = RenderTexture.GetTemporary(width, height);

            for (int side = 0; side < 4; side++)
            {
                TexSide texSide = GetTexSide(side);
                var texCorners = new float2[] { texSide.Left, texSide.Top, texSide.Right };
                float2 center = (texSide.Top + texSide.Left + texSide.Right) / 3;

                Side figureSide = GetSide(side);
                var figureCorners = new float3[] { figureSide.Left, figureSide.Top, figureSide.Right };

                var sideEuler = GetSideAngle(side);

                for (int corner = 0; corner < 3; corner++)
                {
                    Vector3 cornerEuler = GetCornerAngle(corner);
                    float2 offset = (texCorners[corner] - center) / 2;

                    var index = Enumerable.Range(0, 4)
                        .OrderBy(i => distancesq(GetCorner(i), figureCorners[corner]))
                        .First();

                    var position = center + offset;
                    var euler = sideEuler + cornerEuler;
                    var scale = Vector2.one * 0.5f;

                    renderer.RenderSide(temporaryTexture, index, position, euler, scale);
                }
            }
            Graphics.CopyTexture(temporaryTexture, texture);

            return texture;
        }

        public void SideRotation(Transform transform, Mesh mesh, int side, Vector3 topDirection, Vector3 forwardDirection)
        {
            var sideVertices = GetSide(side);

            Vector3 center = (Vector3)(sideVertices.Top + sideVertices.Left + sideVertices.Right) / 3;
            var rotation = Quaternion.FromToRotation(center.normalized, forwardDirection);
            transform.rotation *= rotation;

            Vector3 top = GetCorner(side);
            Vector3 topDefaultPosition = rotation * (top - center);
            float angle = Vector3.SignedAngle(topDefaultPosition.normalized, topDirection, forwardDirection);
            transform.RotateAround(transform.TransformPoint(Vector3.zero), forwardDirection, angle);
        }

        public int GetRolledSide(Transform transform, Mesh mesh, Vector3 normal)
        {
            Func<Vector3, float> Distance = (position) => (-normal - Vector3.Scale(position, normal)).sqrMagnitude;

            var topmostVertex = Enumerable.Range(0, 4)
                .OrderByDescending(i => Distance(transform.rotation * GetCorner(i)))
                .First();

            return topmostVertex + 1;
        }

        private static float2 GetTangentXZ(float3 top, float3 p) => ((float3)Vector3.Cross(top, p).normalized).xz;

        public void Execute<S>(int index, S streams) where S : struct, IMeshStreams
        {
            var vertexOffset = index * (Resolution + 1) * (Resolution + 2) / 2;
            var triangleOffset = index * Resolution * Resolution;

            Side corners = GetSide(index);
            TexSide texCorners = GetTexSide(index);

            var vertex = new Vertex();
            vertex.tangent.w = -1f;

            vertex.position = vertex.normal = corners.Left;
            vertex.texCoord0 = texCorners.Left;
            vertex.tangent.xz = GetTangentXZ(corners.Top, vertex.position);

            var left = vertex;

            vertex.position = vertex.normal = corners.Top;
            vertex.texCoord0 = texCorners.Top;
            vertex.tangent.xz = GetTangentXZ(corners.Top, vertex.position);

            var top = vertex;

            vertex.position = vertex.normal = corners.Right;
            vertex.texCoord0 = texCorners.Right;
            vertex.tangent.xz = GetTangentXZ(corners.Top, vertex.position);

            var right = vertex;

            ResolutionUtils.FitTriangle(
                (left, vertexOffset), 
                (top, vertexOffset + 1), 
                (right, vertexOffset + 2), 
                streams, Resolution, triangleOffset
            );
        }
    }
}