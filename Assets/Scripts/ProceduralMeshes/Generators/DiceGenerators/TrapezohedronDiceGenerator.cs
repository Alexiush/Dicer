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
        public int VertexCount => ActualDieSize * (((Resolution + 1) * (Resolution + 1) + (Resolution + 1)));
        public int IndexCount => 6 * ActualDieSize * Resolution * Resolution;
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
            bool validSize = DieSize % 4 != 0 && DieSize >= 6;
            bool validResolution = Resolution > 0;

            return validSize && validResolution;
        }

        private float BaseDelta => (1 - sin((90 - Angle / 2) * Mathf.Deg2Rad)) / (1 + sin((90 - Angle / 2) * Mathf.Deg2Rad));
        private static readonly float _apexDelta = 1.0f;
        private float _texTiling => 1.0f / (ActualDieSize + 2);
        public MeshJobScheduleDelegate DefaultJobHandle => MeshJob<TrapezohedronDiceGenerator, MultiStream>.ScheduleParallel;
        public DieMeshJobScheduleDelegate DefaultDieJobHandle => DieMeshJob<TrapezohedronDiceGenerator, MultiStream>.ScheduleParallel;

        private float Angle => 360f / (ActualDieSize / 2);
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

            var size = 3f / (ActualDieSize / 2 + 1 / 2f);

            var initialFontSize = text.fontSize;

            for (int i = 0; i < ActualDieSize; i++)
            {
                float2 center;
                Vector3 euler;
                float totalOffset = offset * (2 * ((i % (ActualDieSize / 2)) + 1f / 2));

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
            Func<IEnumerable<int>, Vector3> SideCenter = (indices) => indices
                .Select(index => transform.rotation * mesh.vertices[index])
                .Aggregate((acc, e) => acc + e) / 6;

            var topmostSide = mesh.triangles
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / 6)
                .Select(kv => (kv.Key, kv.ToList()))
                .OrderByDescending(triangle => SideCenter(triangle.Item2.Select(v => v.Value)).y)
                .First().Key;

            return (topmostSide) / (Resolution * Resolution) + 1;
        }

        public void Execute<S>(int i, S streams) where S : struct, IMeshStreams
        {
            var vertex = new Vertex();
            vertex.tangent.w = -1f;

            int mainVertexOffset = i * ((Resolution + 1) * (Resolution + 1) + (Resolution + 1));
            int inverseMainVertexOffset = VertexCount / 2 + mainVertexOffset;

            int secondaryVertexOffset = i * ((Resolution + 1) * (Resolution + 1) + (Resolution + 1)) + ((Resolution + 1) * (Resolution + 1) + (Resolution + 1)) / 2;
            int inverseSecondaryVertexOffset = VertexCount / 2 + secondaryVertexOffset;

            int textureOffset = i * (3 - (i > 0 ? 1 : 0));
            int inverseTextureOffset = 2 * ActualDieSize - textureOffset + 1;

            int triangleOffset = i * 2 * (Resolution * Resolution);
            int inverseTriangleOffset = (ActualDieSize / 2 + i) * 2 * (Resolution * Resolution);

            // Add the first vertex
            vertex.position = GetUpperPolygonVertex(i);

            vertex.normal = normalize(vertex.position);
            vertex.tangent.xz = GetUpperPolygonVertexTangent(i);
            vertex.texCoord0 = GetTexCoord(textureOffset);

            // Add the vertex
            var mainUp = vertex;
            // streams.SetVertex(mainVertexOffset, vertex);
            // Debug.Log($"{mainVertexOffset} main up");

            vertex.texCoord0 = float2(-1, -1);
            var secondaryMainUp = vertex;
            // streams.SetVertex(secondaryVertexOffset, vertex);
            // Debug.Log($"{secondaryVertexOffset} secondary up");
            var inverseSecondaryMainUp = vertex;
            // streams.SetVertex(inverseSecondaryVertexOffset + 2, vertex);
            // Debug.Log($"{inverseSecondaryVertexOffset + 2} inverse secondary next up");

            // Add the next vertex in the middle and it's apex

            vertex.position = GetUpperPolygonVertex(i + 1);

            vertex.normal = normalize(vertex.position);
            vertex.tangent.xz = GetUpperPolygonVertexTangent(i + 1);
            vertex.texCoord0 = GetTexCoord(textureOffset + 2);

            // Add the vertex
            var nextUp = vertex;
            // streams.SetVertex(mainVertexOffset + 2, vertex);
            // Debug.Log($"{mainVertexOffset + 2} next up");

            vertex.texCoord0 = float2(-1, -1);
            var secondaryNextUp = vertex;
            // streams.SetVertex(secondaryVertexOffset + 2, vertex);
            // Debug.Log($"{secondaryVertexOffset + 2} secondary next up");

            // Add it's apex

            vertex.position = Vector3.up * _apexDelta;
            // Normal is above, tangent is forward
            vertex.normal = normalize(vertex.position);
            vertex.tangent = new float4(0, 0, 1, -1);
            vertex.texCoord0 = GetTexCoord(textureOffset + 1);

            var apexUp = vertex;
            // streams.SetVertex(mainVertexOffset + 1, vertex);
            // Debug.Log($"{mainVertexOffset + 1} apex up");

            // Add the triangle to an apex
            ResolutionUtils.FitTriangle((nextUp, mainVertexOffset + 2), (apexUp, mainVertexOffset + 1), (mainUp, mainVertexOffset), streams, Resolution, triangleOffset);
            // streams.SetTriangle(triangleOffset, new int3(mainVertexOffset, mainVertexOffset + 2, mainVertexOffset + 1));
            // Debug.Log($"{triangleOffset}: ({mainVertexOffset}, {mainVertexOffset + 2}, {mainVertexOffset + 1}) main up triangle");

            // Add the start vertex 

            vertex.position = GetBottomPolygonVertex(i);

            vertex.normal = normalize(vertex.position);
            vertex.tangent.xz = GetBottomPolygonVertexTangent(i);
            vertex.texCoord0 = GetTexCoord(inverseTextureOffset);

            // Add the vertex
            var mainDown = vertex;
            // streams.SetVertex(inverseMainVertexOffset, vertex);
            // Debug.Log($"{inverseMainVertexOffset} main down");

            vertex.texCoord0 = float2(-1, -1);
            var secondaryMainDown = vertex;
            // streams.SetVertex(inverseSecondaryVertexOffset, vertex);
            // Debug.Log($"{inverseSecondaryVertexOffset} secondary down");

            vertex.position = GetBottomPolygonVertex(i + 1);

            vertex.normal = normalize(vertex.position);
            vertex.tangent.xz = GetBottomPolygonVertexTangent(i + 1);
            vertex.texCoord0 = GetTexCoord(inverseTextureOffset - 2);

            // Add the vertex
            var nextDown = vertex;
            // streams.SetVertex(inverseMainVertexOffset + 2, vertex);
            // Debug.Log($"{inverseMainVertexOffset + 2} next down");

            vertex.texCoord0 = float2(-1, -1);
            var secondaryNextDown = vertex;
            // streams.SetVertex(inverseSecondaryVertexOffset + 1, vertex);
            // Debug.Log($"{inverseSecondaryVertexOffset + 1} secondary next down");
            var inverseSecondaryMainDown = vertex;
            // streams.SetVertex(secondaryVertexOffset + 1, vertex);
            // Debug.Log($"{secondaryVertexOffset + 1} inverse main down");

            vertex.position = Vector3.down * _apexDelta;
            // Normal is below, tangent is backward
            vertex.normal = normalize(vertex.position);
            vertex.tangent = new float4(0, 0, -1, -1);
            vertex.texCoord0 = GetTexCoord(inverseTextureOffset - 1);

            var apexDown = vertex;
            // streams.SetVertex(inverseMainVertexOffset + 1, vertex);
            // Debug.Log($"{inverseMainVertexOffset + 1} apex down");

            ResolutionUtils.FitTriangle((mainDown, inverseMainVertexOffset), (apexDown, inverseMainVertexOffset + 1), (nextDown, inverseMainVertexOffset + 2), streams, Resolution, inverseTriangleOffset);
            // streams.SetTriangle(inverseTriangleOffset, new int3(inverseMainVertexOffset, inverseMainVertexOffset + 1, inverseMainVertexOffset + 2));
            // Debug.Log($"{inverseTriangleOffset}: ({inverseMainVertexOffset}, {inverseMainVertexOffset + 1}, {inverseMainVertexOffset + 2}) main down triangle");

            // Add the triangles to opposites
            ResolutionUtils.FitTriangle((secondaryMainUp, secondaryVertexOffset), (inverseSecondaryMainDown, secondaryVertexOffset + 1), (secondaryNextUp, secondaryVertexOffset + 2), streams, Resolution, triangleOffset + (Resolution * Resolution));
            // streams.SetTriangle(triangleOffset + 1, new int3(secondaryVertexOffset, secondaryVertexOffset + 1, secondaryVertexOffset + 2));
            // Debug.Log($"{triangleOffset + 1}: ({secondaryVertexOffset}, {secondaryVertexOffset + 1}, {secondaryVertexOffset + 2}) secondary up triangle");
            ResolutionUtils.FitTriangle((secondaryMainDown, inverseSecondaryVertexOffset), (inverseSecondaryMainUp, inverseSecondaryVertexOffset + 1), (secondaryNextDown, inverseSecondaryVertexOffset + 2), streams, Resolution, inverseTriangleOffset + (Resolution * Resolution));
            // streams.SetTriangle(inverseTriangleOffset + 1, new int3(inverseSecondaryVertexOffset, inverseSecondaryVertexOffset + 1, inverseSecondaryVertexOffset + 2));
            // Debug.Log($"{inverseTriangleOffset + 1}: ({inverseSecondaryVertexOffset}, {inverseSecondaryVertexOffset + 1}, {inverseSecondaryVertexOffset + 2}) secondary down triangle");
        }
    }
}