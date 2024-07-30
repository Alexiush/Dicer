using UnityEngine;

namespace Dicer.TextureGeneration
{
    public abstract class MeshSupplier : MonoBehaviour
    {
        public abstract Mesh GetMesh(int sideIndex);
        public virtual Material Material { get; private set; }
    }
}
