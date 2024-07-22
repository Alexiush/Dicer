using ProceduralMeshes;
using ProceduralMeshes.Streams;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public static class ResolutionUtils
{
    private static int VertexLevel(int level) => level * (level + 1) / 2;

    // Code to fit the triangle to an expected resolution is generic
    public static void FitTriangle<S>((Vertex v, int i) left, (Vertex v, int i) top, (Vertex v, int i) right, 
        S stream, int resolution, int triangleOffset) where S : struct, IMeshStreams
    {
        // Vertex index just increments after the highest index among three vertices
        // Triangle index just increments after specified number
        int vertexOffset = max(left.i, max(top.i, right.i)) + 1;

        // Vertex positions and their texture positions are interpolated as in tetrahedron code
        // For now the same applies to normals and tangents

        var vertex = new Vertex();
        int trianglesCount = 0;

        int triangleIndex() => triangleOffset + trianglesCount;
        int verifiedVertexIndex(int index)
        {
            if (index == 0)
            {
                return top.i;
            }
            else if (index == VertexLevel(resolution))
            {
                return left.i;
            }
            else if (index == VertexLevel(resolution + 1) - 1)
            {
                return right.i;
            }
            else
            {
                return vertexOffset + index - (index > VertexLevel(resolution) ? 2 : 1);
            }
        }

        // Cycle iterates over vertices
        for (int level = 0; level <= resolution; level++)
        {
            for (int v = 0; v < level + 1; v++)
            {
                int vertexIndex = VertexLevel(level) + v;

                if (v == 0 && level == 0)
                {
                    vertex = top.v;
                }
                else if (v == 0 && level == resolution)
                {
                    vertex = left.v;
                }
                else if (v == level && level == resolution)
                {
                    vertex = right.v;
                }
                else
                {
                    var leftPos = top.v.position + (left.v.position - top.v.position) * level / resolution;
                    var rightPos = top.v.position + (right.v.position - top.v.position) * level / resolution;
                    var deltaPos = (rightPos - leftPos) / level;
                    var leftOffsetPos = v;

                    vertex.position = leftPos + deltaPos * leftOffsetPos;

                    var leftTex = top.v.texCoord0 + (left.v.texCoord0 - top.v.texCoord0) * level / resolution;
                    var rightTex = top.v.texCoord0 + (right.v.texCoord0 - top.v.texCoord0) * level / resolution;
                    var deltaTex = (rightTex - leftTex) / level;
                    var leftOffsetTex = v;

                    vertex.texCoord0 = leftTex + deltaTex * leftOffsetTex;

                    vertex.normal = vertex.position;

                    var leftTan = top.v.tangent + (left.v.tangent - top.v.tangent) * level / resolution;
                    var rightTan = top.v.tangent + (right.v.tangent - top.v.tangent) * level / resolution;
                    var deltaTan = (rightTan - leftTan) / level;
                    var leftOffsetTan = v;

                    vertex.tangent = leftTan + deltaTan * leftOffsetTan;
                }
                stream.SetVertex(verifiedVertexIndex(vertexIndex), vertex);

                if (v > 0)
                {
                    // Add the common triangle
                    stream.SetTriangle(triangleIndex(), new int3(
                        verifiedVertexIndex(vertexIndex), 
                        verifiedVertexIndex(vertexIndex - level - 1),
                        verifiedVertexIndex(vertexIndex - 1)
                    ));
                    trianglesCount++;

                    if (v < level)
                    {
                        // Add the inverted triangle
                        stream.SetTriangle(triangleIndex(), new int3(
                            verifiedVertexIndex(vertexIndex),
                            verifiedVertexIndex(vertexIndex - level),
                            verifiedVertexIndex(vertexIndex - level - 1)
                        ));
                        trianglesCount++;
                    }
                }
            }
        }
    }
}
