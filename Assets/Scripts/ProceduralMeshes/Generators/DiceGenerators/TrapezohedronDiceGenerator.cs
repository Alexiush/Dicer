using Nothke.Utils;
using ProceduralMeshes.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace ProceduralMeshes.Generators
{
    public struct TrapezohedronDiceGenerator : IDiceGenerator
    {
        public int VertexCount => 2 * (3 * Resolution + 2);
        public int IndexCount => 12 * Resolution;
        public int JobLength => Resolution;
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
        public int Resolution { get; set; }

        private float BaseDelta => (1 - sin((90 - Angle / 2) * Mathf.Deg2Rad)) / (1 + sin((90 - Angle / 2) * Mathf.Deg2Rad));
        private static readonly float _apexDelta = 1.0f;
        private readonly float _texTiling => 1.0f / (Resolution + 2);
        public MeshJobScheduleDelegate DefaultJobHandle => MeshJob<TrapezohedronDiceGenerator, MultiStream>.ScheduleParallel;

        private float Angle => 360f / Resolution;
        private float Shift => Angle / 2;
        private float3 GetUpperPolygonVertex(int i) => new float3(
            - sin(Angle * i * Mathf.Deg2Rad),
            BaseDelta,
            cos(Angle * i * Mathf.Deg2Rad)
        );
        private float2 GetUpperPolygonVertexTangent(int i) => new float2(
            -sin((Angle * i + 90f) * Mathf.Deg2Rad),
            cos((Angle * i + 90f) * Mathf.Deg2Rad)
        );

        private float3 GetBottomPolygonVertex(int i) => new float3(
           -sin((Angle * i - Shift) * Mathf.Deg2Rad),
           - BaseDelta,
           cos((Angle * i - Shift) * Mathf.Deg2Rad)
        );
        private float2 GetBottomPolygonVertexTangent(int i) => new float2(
            -sin((Angle * i - Shift + 90f) * Mathf.Deg2Rad),
            cos((Angle * i - Shift + 90f) * Mathf.Deg2Rad)
        );

        private float2 GetTexCoord(int i)
        {
            if (i > Resolution * 2)
            {
                i -= Resolution * 2;
            }

            if (i % 2 == 0)
            {
                return new float2(1, 1 - i / (2 * (Resolution + 1f / 2)));
            }
            else
            {
                return new float2(0, (2 * Resolution + 1 - i) / (2 * (Resolution + 1f / 2)));
            }
        }

        public Texture2D GenerateNumbersTexture(int width, int height, TMP_Text text)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            var temporaryTexture = RenderTexture.GetTemporary(width, height);

            temporaryTexture.BeginOrthoRendering();
            float offset = 1f / (2 * Resolution + 1);

            var size = 3f / (Resolution + 1 / 2f);

            var initialFontSize = text.fontSize;

            for (int i = 0; i < Resolution * 2; i++)
            {
                float2 center;
                Vector3 euler;
                float totalOffset = offset * (2 * ((i % Resolution) + 1f / 2));

                if (i < Resolution)
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
            Func<IEnumerable<int>, Vector3> SideCenter = (indices) => indices
                .Select(index => transform.rotation * mesh.vertices[index])
                .Aggregate((acc, e) => acc + e) / 6;

            var topmostSide = mesh.triangles
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / 6)
                .Select(kv => (kv.Key, kv.ToList()))
                .OrderByDescending(triangle => SideCenter(triangle.Item2.Select(v => v.Value)).y)
                .First().Key;

            return topmostSide + 1;
        }

        public void Execute<S>(int i, S streams) where S : struct, IMeshStreams
        {

            // if (i == 0) add the initial middle vertex for both sides

            var vertex = new Vertex();
            vertex.tangent.w = -1f;

            if (i == 0)
            {
                vertex.position = GetUpperPolygonVertex(i);

                vertex.normal = normalize(vertex.position);
                vertex.tangent.xz = GetUpperPolygonVertexTangent(i);
                vertex.texCoord0 = GetTexCoord(i);

                // Add the vertex
                streams.SetVertex(i, vertex);

                vertex.texCoord0 = float2(-1, -1);
                streams.SetVertex(i + 1, vertex);

                vertex.position = GetBottomPolygonVertex(i);

                vertex.normal = normalize(vertex.position);
                vertex.tangent.xz = GetBottomPolygonVertexTangent(i);
                vertex.texCoord0 = GetTexCoord(4 * Resolution + 1);

                // Add the vertex
                streams.SetVertex(VertexCount - 1, vertex);

                vertex.texCoord0 = float2(-1, -1);
                streams.SetVertex(VertexCount - 2, vertex);
            }

            // Add the next vertex in the middle and it's apex

            int vertexOffset = i * 3 + 2;
            int inverseVertexOffset = VertexCount - 1 - vertexOffset;

            int textureOffset = i * 2 + 1;
            int inverseTextureOffset = 4 * Resolution + 1 - textureOffset;

            vertex.position = GetUpperPolygonVertex((i + 1) % Resolution);

            vertex.normal = normalize(vertex.position);
            vertex.tangent.xz = GetUpperPolygonVertexTangent((i + 1) % Resolution);
            vertex.texCoord0 = GetTexCoord(textureOffset + 1);

            // Add the vertex
            streams.SetVertex(vertexOffset + 1, vertex);

            vertex.texCoord0 = float2(-1, -1);
            streams.SetVertex(vertexOffset + 2, vertex);
            // Add the triangle to it's opposite
            streams.SetTriangle(i * 2 + 1, new int3(vertexOffset + 2, vertexOffset - 1, inverseVertexOffset - 2));

            // Add it's apex

            vertex.position = Vector3.up * _apexDelta;
            // Normal is above, tangent is forward
            vertex.normal = normalize(vertex.position);
            vertex.tangent = new float4(0, 0, 1, -1);
            vertex.texCoord0 = GetTexCoord(textureOffset);

            streams.SetVertex(vertexOffset, vertex);

            // Add the triangle to an apex
            streams.SetTriangle(i * 2, new int3(vertexOffset, vertexOffset - 2, vertexOffset + 1));

            vertex.position = GetBottomPolygonVertex((i + 1) % Resolution);

            vertex.normal = normalize(vertex.position);
            vertex.tangent.xz = GetBottomPolygonVertexTangent((i + 1) % Resolution);
            vertex.texCoord0 = GetTexCoord(inverseTextureOffset - 1);

            // Add the vertex
            streams.SetVertex(inverseVertexOffset - 1, vertex);

            vertex.texCoord0 = float2(-1, -1);
            streams.SetVertex(inverseVertexOffset - 2, vertex);

            // Add the triangle to it's opposite
            streams.SetTriangle(2 * (Resolution + i) + 1, new int3(inverseVertexOffset - 2, vertexOffset - 1, inverseVertexOffset + 1));

            vertex.position = Vector3.down * _apexDelta;
            // Normal is below, tangent is backward
            vertex.normal = normalize(vertex.position);
            vertex.tangent = new float4(0, 0, -1, -1);
            vertex.texCoord0 = GetTexCoord(inverseTextureOffset);

            streams.SetVertex(inverseVertexOffset, vertex);

            streams.SetTriangle(2 * (Resolution + i), new int3(inverseVertexOffset - 1, inverseVertexOffset + 2, inverseVertexOffset));
            
        }
    }
}