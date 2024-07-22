using Nothke.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBasedSideTextureRenderer : SideTextureRenderer
{
    [SerializeField]
    private MeshSupplier _meshSupplier;

    public override RenderTexture RenderSide(RenderTexture canvas, int sideIndex, Vector2 position, Vector3 euler, Vector2 scale)
    {
        canvas.BeginOrthoRendering();

        Mesh mesh = _meshSupplier.GetMesh(sideIndex);
        Material material = _meshSupplier.Material;
        canvas.DrawMesh(mesh, material, Matrix4x4.TRS((Vector3)position, Quaternion.Euler(euler), new Vector3(scale.x, scale.y, 1)));
        
        canvas.EndRendering();

        return canvas;
    }
}
