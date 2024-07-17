using ProceduralMeshes;
using ProceduralMeshes.Generators;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public interface IDiceGenerator : IMeshGenerator
{
    public int DieSize {  get; set; }
    public int ActualDieSize { get; }

    public bool Validate();

    public Texture2D GenerateNumbersTexture(int width, int height, SideTextureRenderer renderer);

    public int GetSelectedSide(Transform transform, Mesh mesh);

    public DieMeshJobScheduleDelegate DefaultDieJobHandle { get; }
}
