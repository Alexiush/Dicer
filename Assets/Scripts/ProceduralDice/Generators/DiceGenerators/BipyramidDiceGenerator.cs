using Nothke.Utils;
using ProceduralMeshes.Streams;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace ProceduralMeshes.Generators
{
    public struct BipyramidDiceGenerator : IDiceGenerator
    {
        public int VertexCount => ActualDieSize * ((Resolution + 1) * (Resolution + 1) + (Resolution + 1)) / 2;
        public int IndexCount => 3 * ActualDieSize * Resolution * Resolution;
        public int JobLength => ActualDieSize / 2;
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
        public int Resolution { get; set; }
        public int DieSize { get; set; }
        public int ActualDieSize => DieSize;

        public bool Validate()
        {
            bool validSize = ActualDieSize % 4 == 0 && ActualDieSize >= 8;
            bool validResolution = Resolution > 0;

            return validSize && validResolution;
        }

        private static readonly float _apexDelta = 1.0f;
        public DieMeshJobScheduleDelegate DefaultDieJobHandle => DieMeshJob<BipyramidDiceGenerator, MultiStream>.ScheduleParallel;

        private float Angle => 360f / (ActualDieSize / 2);
        private float3 GetPolygonVertexPosition(int i) => new float3(
            -sin(Angle * i * Mathf.Deg2Rad),
            0,
            cos(Angle * i * Mathf.Deg2Rad)
        );
        private float2 GetPolygonVertexTangent(int i) => new float2(
            -sin((Angle * i + 90f) * Mathf.Deg2Rad),
            cos((Angle * i + 90f) * Mathf.Deg2Rad)
        );

        private float2 GetTexCoord(int i)
        {
            if (i > ActualDieSize)
            {
                i -= ActualDieSize;
            }

            if (i % 2 == 0)
            {
                return new float2(1, 1 - i / (2 * (ActualDieSize / 2 + 1f / 2)));
            }
            else
            {
                return new float2(0, (ActualDieSize + 1 - i) / (2 * (ActualDieSize / 2 + 1f / 2)));
            }
        }

        public Texture2D GenerateNumbersTexture(int width, int height, TMP_Text text)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            var temporaryTexture = RenderTexture.GetTemporary(width, height);

            temporaryTexture.BeginOrthoRendering();
            float offset = 1f / (ActualDieSize + 1);

            var size = 3f / (ActualDieSize / 2 + 1 / 4f);

            var initialFontSize = text.fontSize;

            for (int i = 0; i < ActualDieSize; i++)
            {
                float2 center;
                Vector3 euler;
                float totalOffset = offset * 2 * ((i % (ActualDieSize / 2)) + 1f/2);

                if (i < ActualDieSize / 2)
                {
                    // Upper pass

                    center = new float2(2f / 3f, 1 - totalOffset);
                    euler = new Vector3(180, 0, 90);
                }
                else
                {
                    // Lower pass

                    center = new float2(1f / 3f, 0 + totalOffset);
                    euler = new Vector3(180, 180, -90);
                }

                text.text = (i + 1).ToString();
                text.fontSize = initialFontSize * size / text.text.Length;
                text.ForceMeshUpdate(true, true);

                var position = new Vector3(center.x, center.y, 0);

                temporaryTexture.DrawTMPText(text, Matrix4x4.TRS(position, Quaternion.Euler(euler), new Vector3(1, 2, 1)));
            }

            temporaryTexture.EndRendering();
            Graphics.CopyTexture(temporaryTexture, texture);

            text.text = string.Empty;
            text.fontSize = initialFontSize;
            temporaryTexture.Release();

            return texture;
        }

        public int GetSelectedSide(Transform transform, Mesh mesh)
        {
            Func<IEnumerable<int>, Vector3> TriangleCenter = (indices) => indices
                .Select(index => transform.rotation * mesh.vertices[index])
                .Aggregate((acc, e) => acc + e) / 3;

            var topmostTriangle = mesh.triangles
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / 3)
                .Select(kv => (kv.Key, kv.ToList()))
                .OrderByDescending(triangle => TriangleCenter(triangle.Item2.Select(v => v.Value)).y)
                .First().Key;

            return (topmostTriangle) / (Resolution * Resolution) + 1;
        }

        public void Execute<S>(int i, S streams) where S : struct, IMeshStreams
        {
            var vertex = new Vertex();
            vertex.tangent.w = -1f;

            int delta = ((Resolution + 1) * (Resolution + 1) + (Resolution + 1)) / 2 - 3;
            
            int vertexOffset = i * (3 + delta);
            int textureOffset = i * (3 - (i > 0 ? 1 : 0));
            int triangleOffset = i * Resolution * Resolution;

            int inverseVertexOffset = VertexCount / 2 + vertexOffset;
            int inverseTextureOffset = 2 * ActualDieSize - textureOffset + 1;
            int inverseTriangleOffset = (i + ActualDieSize / 2) * Resolution * Resolution;

            vertex.position = vertex.normal = GetPolygonVertexPosition(i);
            vertex.tangent.xz = GetPolygonVertexTangent(i);
            vertex.texCoord0 = GetTexCoord(textureOffset);
            var right = vertex;

            vertex.position = vertex.normal = GetPolygonVertexPosition(i + 1);
            vertex.tangent.xz = GetPolygonVertexTangent(i + 1);
            vertex.texCoord0 = GetTexCoord(textureOffset + 2);
            var left = vertex;

            vertex.position = vertex.normal = Vector3.up * _apexDelta;
            vertex.tangent = new float4(0, 0, 1, -1);
            vertex.texCoord0 = GetTexCoord(textureOffset + 1);
            var top = vertex;

            ResolutionUtils.FitTriangle(
                (left, vertexOffset + 2), 
                (top, vertexOffset + 1), 
                (right, vertexOffset), 
                streams, 
                Resolution, 
                triangleOffset
            );

            vertex.position = vertex.normal = GetPolygonVertexPosition(i);
            vertex.tangent.xz = GetPolygonVertexTangent(i);
            vertex.texCoord0 = GetTexCoord(inverseTextureOffset);
            left = vertex;

            vertex.position = vertex.normal = GetPolygonVertexPosition(i + 1);
            vertex.tangent.xz = GetPolygonVertexTangent(i + 1);
            vertex.texCoord0 = GetTexCoord(inverseTextureOffset - 2);
            right = vertex;

            vertex.position = vertex.normal = Vector3.down * _apexDelta;
            vertex.tangent = new float4(0, 0, -1, -1);
            vertex.texCoord0 = GetTexCoord(inverseTextureOffset - 1);
            top = vertex;

            ResolutionUtils.FitTriangle(
                (left, inverseVertexOffset), 
                (top, inverseVertexOffset + 1), 
                (right, inverseVertexOffset + 2), 
                streams, 
                Resolution, 
                inverseTriangleOffset
            );
        }
    }
}