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
        public int ActualDieSize
        {
            get
            {
                return DieSize % 2 == 0 ? DieSize : DieSize * 2;
            }
        }

        public bool Validate()
        {
            bool validSize = DieSize % 4 == 0 && DieSize >= 8;
            bool validResolution = Resolution > 0;

            return validSize && validResolution;
        }

        private static readonly float _apexDelta = 1.0f;
        private float _texTiling => 1.0f / (ActualDieSize + 2);
        public MeshJobScheduleDelegate DefaultJobHandle => MeshJob<BipyramidDiceGenerator, MultiStream>.ScheduleParallel;
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
            // if (i == 0) add the initial middle vertex for both sides

            var vertex = new Vertex();
            vertex.tangent.w = -1f;

            int delta = ((Resolution + 1) * (Resolution + 1) + (Resolution + 1)) / 2 - 3;
            int vertexOffset = i * (3 + delta);
            int texOffset = i * (3 - (i > 0 ? 1 : 0));
            int tiOffset = i * Resolution * Resolution;

            // Debug.Log($"{i} {texOffset}");

            // Add the right vertex

            vertex.position = GetPolygonVertexPosition(i);

            vertex.normal = normalize(vertex.position);
            vertex.tangent.xz = GetPolygonVertexTangent(i);
            vertex.texCoord0 = GetTexCoord(texOffset);
            // Debug.Log($"{vertexOffset} up start {texOffset}");

            // Add the vertex
            // streams.SetVertex(vertexOffset, vertex);
            var right = vertex;
            // Debug.Log($"{i} up start");

            // Add the next vertex in the middle and it's apex
            vertex.position = GetPolygonVertexPosition((i + 1) % ActualDieSize);

            vertex.normal = normalize(vertex.position);
            vertex.tangent.xz = GetPolygonVertexTangent((i + 1) % ActualDieSize);
            vertex.texCoord0 = GetTexCoord(texOffset + 2);
            // Debug.Log($"{vertexOffset + 2} up next {texOffset + 2}");

            // Add the vertex
            // streams.SetVertex(vertexOffset + 2, vertex);
            var left = vertex;
            // Debug.Log($"{vertexOffset + 1} up next");

            // Add both of it's apex

            vertex.position = Vector3.up * _apexDelta;
            // Normal is above, tangent is forward
            vertex.normal = normalize(vertex.position);
            vertex.tangent = new float4(0, 0, 1, -1);
            vertex.texCoord0 = GetTexCoord(texOffset + 1);
            // Debug.Log($"{vertexOffset + 1} up apex {texOffset + 1}");

            // streams.SetVertex(vertexOffset + 1, vertex);
            var top = vertex;
            // Debug.Log($"{vertexOffset} up apex");

            // Add the triangles from both sides to their apex
            // streams.SetTriangle(i, new int3(vertexOffset + 1, vertexOffset, vertexOffset + 2));
            ResolutionUtils.FitTriangle((left, vertexOffset + 2), (top, vertexOffset + 1), (right, vertexOffset), streams, Resolution, tiOffset);

            int inverseVertexOffset = VertexCount / 2 + vertexOffset;
            int inverseTexOffset = 2 * ActualDieSize - texOffset + 1;
            int inverseTiOffset = (i + ActualDieSize / 2) * Resolution * Resolution;

            // Add the left vertex

            vertex.position = GetPolygonVertexPosition(i);

            vertex.normal = normalize(vertex.position);
            vertex.tangent.xz = GetPolygonVertexTangent(i);
            vertex.texCoord0 = GetTexCoord(inverseTexOffset);
            // Debug.Log($"{inverseVertexOffset} bottom start {inverseTexOffset}");

            // Add the vertex
            // streams.SetVertex(inverseVertexOffset, vertex);
            left = vertex;
            // Debug.Log($"{VertexCount / 2} bottom start");

            vertex.position = GetPolygonVertexPosition((i + 1) % ActualDieSize);

            vertex.normal = normalize(vertex.position);
            vertex.tangent.xz = GetPolygonVertexTangent((i + 1) % ActualDieSize);
            vertex.texCoord0 = GetTexCoord(inverseTexOffset - 2);
            // Debug.Log($"{inverseVertexOffset - 2} bottom next {inverseTexOffset - 2}");

            // Add the vertex
            // streams.SetVertex(inverseVertexOffset + 2, vertex);
            right = vertex;
            // Debug.Log($"{inverseVertexOffset + 2} bottom next");

            vertex.position = Vector3.down * _apexDelta;
            // Normal is below, tangent is backward
            vertex.normal = normalize(vertex.position);
            vertex.tangent = new float4(0, 0, -1, -1);
            vertex.texCoord0 = GetTexCoord(inverseTexOffset - 1);
            // Debug.Log($"{inverseVertexOffset - 1} bottom apex {inverseTexOffset + 1}");

            // streams.SetVertex(inverseVertexOffset + 1, vertex);
            top = vertex;
            // Debug.Log($"{inverseVertexOffset} bottom apex");

            // streams.SetTriangle(ActualDieSize / 2 + i, new int3(inverseVertexOffset + 1, inverseVertexOffset + 2 , inverseVertexOffset));
            ResolutionUtils.FitTriangle((left, inverseVertexOffset), (top, inverseVertexOffset + 1), (right, inverseVertexOffset + 2), streams, Resolution, inverseTiOffset);
        }
    }
}