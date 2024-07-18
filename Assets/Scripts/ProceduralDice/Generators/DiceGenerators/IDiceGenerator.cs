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
    public void SideRotation(Transform transform, Mesh mesh, int side, Vector3 topDirection, Vector3 forwardDirection);
    public int GetRolledSide(Transform transform, Mesh mesh, Vector3 normal);
    
    public DieMeshJobScheduleDelegate DefaultDieJobHandle { get; }
}