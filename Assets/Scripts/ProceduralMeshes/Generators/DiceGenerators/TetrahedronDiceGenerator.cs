using Nothke.Utils;
using ProceduralMeshes.Streams;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace ProceduralMeshes.Generators
{
    public struct TetrahedronDiceGenerator : IDiceGenerator
    {
        public int VertexCount => 2 * (Resolution + 1) * (Resolution + 2);
        public int IndexCount => 12 * Resolution * Resolution;
        public int JobLength => Resolution * Resolution;
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
        public int Resolution { get; set; }

        public MeshJobScheduleDelegate DefaultJobHandle => MeshJob<TetrahedronDiceGenerator, MultiStream>.ScheduleParallel;

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

        private struct Triangle
        {
            public int Zeroth;
            public int First;
            public int Second;

            public int3 Front => new int3(Zeroth, Second, First);
            public int3 Back => new int3(Zeroth, First, Second);
        }

        private static Side GetSide(int side)
        {
            return side switch
            {
                0 => new Side { Top = GetCorner(0), Left = GetCorner(1), Right = GetCorner(2) },
                1 => new Side { Top = GetCorner(0), Left = GetCorner(3), Right = GetCorner(1) },
                2 => new Side { Top = GetCorner(0), Left = GetCorner(2), Right = GetCorner(3) },
                3 => new Side { Top = GetCorner(1), Left = GetCorner(2), Right = GetCorner(3) },
                _ => throw new System.Exception($"Tetrahedron has only four sides, indexed from 0 to 3. Received argument: {side}")
            };
        }

        // Texture can be applied simply
        // One of the unwraps (not distorted one) is another triangle
        // Another one (distorted, but even simpler) is a square, where sides can be defined again via texture coordinates

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

        private static Vector3 GetNumberAngle(int side)
        {
            return side switch
            {
                0 => new Vector3(0, 180, 0),
                1 => new Vector3(180, 0, -120),
                2 => new Vector3(180, 0, 120),
                3 => new Vector3(180, 180, -120),
                _ => throw new System.Exception($"Tetrahedron has only four sides, indexed from 0 to 3. Received argument: {side}")
            };
        }

        public Texture2D GenerateNumbersTexture(int width, int height, TMP_Text text)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            var temporaryTexture = RenderTexture.GetTemporary(width, height);

            temporaryTexture.BeginOrthoRendering();

            for (int i = 0; i < 4; i++)
            {
                TexSide texSide = GetTexSide(i);
                float2 center = (texSide.Top + texSide.Left + texSide.Right) / 3;
                Vector3 euler = GetNumberAngle(i);

                // Draw the number i+1 in the center
                // Set textMeshPro text and direction
                text.text = (i + 1).ToString();
                text.ForceMeshUpdate(true, true);

                var position = new Vector3(center.x, center.y, 0);

                temporaryTexture.DrawTMPText(text, Matrix4x4.TRS(position, Quaternion.Euler(euler), Vector3.one));
            }

            temporaryTexture.EndRendering();
            Graphics.CopyTexture(temporaryTexture, texture);

            text.text = string.Empty;
            temporaryTexture.Release();

            return texture;
        }

        // Sides (by corners) are:
        // 1 - 1, 2, 3
        // 2 - 1, 2, 4
        // 3 - 1, 3, 4
        // 4 - 2, 3, 4

        private static int TriangleLevel(int level) => level * level;

        private int GetTriangleLevel(int index)
        {
            int level = 0;

            for (int i = 1; i <= Resolution; ++i)
            {
                if (TriangleLevel(i) > index)
                {
                    break;
                }

                level++;
            }

            return level;
        }

        private static int VertexLevel(int level) => level * (level + 1) / 2;

        private int GetVertexLevel(int index)
        {
            int level = 0;

            for (int i = 1; i <= Resolution; ++i)
            {
                if (VertexLevel(i) > index)
                {
                    break;
                }

                level++;
            }

            return level;
        }

        // Get offset by the index
        // The tetrahedron is inside a unit sphere
        // 3 of it sides start from the top (0, 1, 0)
        // They go towards 3 vertices that lie down:
        // (0, 1 - sqrt(8/3), -1), (-sqrt(3/2), 1 - sqrt(8/3), 1/2), (sqrt(3/2), 1 - sqrt(8/3), 1/2)
        // The length of the side is sqrt(8/3)

        // Get the top offset by getting the level on which current triangle is
        // Get the left offset based on the triangle's place on that level
        private float3 GetVertex(int sideIndex, int index)
        {
            // Get this side's corners
            var side = GetSide(sideIndex);

            var level = GetVertexLevel(index);

            if (level == 0)
            {
                return side.Top;
            }

            // Get the left and right sides of the level baseline
            var leftSide = side.Top + (side.Left - side.Top) * level / Resolution;
            var rightSide = side.Top + (side.Right - side.Top) * level / Resolution;

            // Get the vertex based on it's position on the line
            var triangleSide = (rightSide - leftSide) / level;
            var leftOffset = index - VertexLevel(level);

            var position = leftSide + triangleSide * leftOffset;

            return position;
        }

        private float2 GetVertexTex(int sideIndex, int index)
        {
            // Get this side's corners
            var side = GetTexSide(sideIndex);

            var level = GetVertexLevel(index);

            if (level == 0)
            {
                return side.Top;
            }

            // Get the left and right sides of the level baseline
            var leftSide = side.Top + (side.Left - side.Top) * level / Resolution;
            var rightSide = side.Top + (side.Right - side.Top) * level / Resolution;

            // Get the vertex based on it's position on the line
            var triangleSide = (rightSide - leftSide) / level;
            var leftOffset = index - VertexLevel(level);

            var position = leftSide + triangleSide * leftOffset;

            return position;
        }

        private static float2 GetTangentXZ(float3 p) => normalize(float2(-p.z, p.x));

        public void Execute<S>(int index, S streams) where S : struct, IMeshStreams
        {
            // Each job creates nth triangle on each side 

            // For each side
            for (int side = 0; side < 4; side++)
            {
                // Get the current set of vertices

                var viOffset = side * (Resolution + 1) * (Resolution + 2) / 2;
                var tiOffset = side * Resolution * Resolution;

                // If the index is 0 all three indices added
                // If the index is of the leftmost triangle on the level it's left and right indices are needed
                // If the index is of the even triangle on level only the right vertex is needed
                // If the index is of the odd triangle on level no new vertices are needed

                var level = GetTriangleLevel(index);
                var leftOffset = index - TriangleLevel(level);
                var vertex = new Vertex();
                var triangle = new Triangle(); 

                // Normal and tangent vectors must be the same for one side
                // So they can be safely stated after the vertex creation
                
                var corners = GetSide(side);
                vertex.normal = GetCorner(side);
                // vertex.normal = Vector3.Cross((corners.Left - corners.Top), (corners.Right - corners.Top)).normalized;

                float3 rightDirection = Vector3.Cross((corners.Top - 0), (vertex.normal - 0)).normalized;
                vertex.tangent.xz = rightDirection.xz;
                vertex.tangent.w = -1f;

                if (index == 0)
                {
                    triangle = new Triangle
                    {
                        Zeroth = 0,
                        First = 1,
                        Second = 2,
                    };

                    // Add 0, 1, 2 vertices
                    vertex.position = GetVertex(side, triangle.Zeroth);
                    vertex.texCoord0 = GetVertexTex(side, triangle.Zeroth);
                    if (Resolution == 1)
                    {
                        vertex.normal = vertex.position;
                        vertex.tangent.xz = GetTangentXZ(vertex.position);
                    }

                    streams.SetVertex(viOffset + triangle.Zeroth, vertex);

                    vertex.position = GetVertex(side, triangle.First);
                    vertex.texCoord0 = GetVertexTex(side, triangle.First);
                    if (Resolution == 1)
                    {
                        vertex.normal = vertex.position;
                        vertex.tangent.xz = GetTangentXZ(vertex.position);
                    }

                    streams.SetVertex(viOffset + triangle.First, vertex);

                    vertex.position = GetVertex(side, triangle.Second);
                    vertex.texCoord0 = GetVertexTex(side, triangle.Second);
                    if (Resolution == 1)
                    {
                        vertex.normal = vertex.position;
                        vertex.tangent.xz = GetTangentXZ(vertex.position);
                    }

                    streams.SetVertex(viOffset + triangle.Second, vertex);
                }
                else if (leftOffset == 0)
                {
                    triangle = new Triangle
                    {
                        Zeroth = VertexLevel(level + 1) - level - 1,
                        First = VertexLevel(level + 1),
                        Second = VertexLevel(level + 1) + 1,
                    };

                    // Add _levels[level], _levels[level] + 1
                    vertex.position = GetVertex(side, triangle.First);
                    vertex.texCoord0 = GetVertexTex(side, triangle.First);
                    streams.SetVertex(viOffset + triangle.First, vertex);

                    vertex.position = GetVertex(side, triangle.Second);
                    vertex.texCoord0 = GetVertexTex(side, triangle.Second);
                    streams.SetVertex(viOffset + triangle.Second, vertex);
                }
                else if (leftOffset % 2 == 0)
                {
                    triangle = new Triangle
                    {
                        Zeroth = index - VertexLevel(level - 1) - leftOffset / 2,
                        First = index - VertexLevel(level - 1) - leftOffset / 2 + level + 1,
                        Second = index - VertexLevel(level - 1) - leftOffset / 2 + level + 2,
                    };

                    // Add _levels[level] + leftOffset
                    vertex.position = GetVertex(side, triangle.Second);
                    vertex.texCoord0 = GetVertexTex(side, triangle.Second);
                    streams.SetVertex(viOffset + triangle.Second, vertex);
                }
                else
                {
                    triangle = new Triangle
                    {
                        Zeroth = VertexLevel(level + 1) + (leftOffset + 1) / 2,
                        First = VertexLevel(level + 1) + (leftOffset + 1) / 2 - level - 1,
                        Second = VertexLevel(level + 1) + (leftOffset + 1) / 2 - level - 2,
                    };

                    // Do not add vertices
                }

                streams.SetTriangle(tiOffset + index, viOffset + triangle.Front);
            }
        }
    }
}