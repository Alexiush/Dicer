using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MeshSupplier : MonoBehaviour
{
    public abstract Mesh GetMesh(int sideIndex);
    public virtual Material Material { get; private set; }
}
