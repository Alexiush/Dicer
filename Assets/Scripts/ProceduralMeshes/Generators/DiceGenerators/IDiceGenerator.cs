using ProceduralMeshes.Generators;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public interface IDiceGenerator : IMeshGenerator
{
    public Texture2D GenerateNumbersTexture(int width, int height, TMP_Text text);

    public int GetSelectedSide(Transform transform, Mesh mesh);
}
