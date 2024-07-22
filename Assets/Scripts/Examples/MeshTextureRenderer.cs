using Nothke.Utils;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshTextureRenderer : MonoBehaviour
{
    [SerializeField]
    private MeshFilter _meshFilter;
    [SerializeField] 
    private Material _material;
    [SerializeField]
    private Transform _transform;
    [SerializeField]
    private RenderTexture _renderTexture;

    private void Render()
    {
        var texture = new Texture2D(_renderTexture.width, _renderTexture.height, TextureFormat.ARGB32, false);

        _renderTexture.BeginPerspectiveRendering(60, _transform.position + Vector3.back * 5, Quaternion.identity);
        _renderTexture.DrawMesh(_meshFilter.mesh, _material, Matrix4x4.TRS(_transform.position, _transform.rotation, Vector3.one));

        texture.ReadPixels(new Rect(0, 0, _renderTexture.width, _renderTexture.height), 0, 0);
        texture.Apply();

        _renderTexture.EndRendering();

        File.WriteAllBytes("Assets/Textures/Mesh.png", texture.EncodeToPNG());
    }

    private void Update()
    {
        Render();
        this.enabled = false;
    }
}
