using UnityEngine;

namespace Dicer.TextureGeneration
{
    public abstract class SideTextureRenderer : MonoBehaviour
    {
        public abstract RenderTexture RenderSide(RenderTexture canvas, int sideIndex, Vector2 position, Vector3 euler, Vector2 scale);
    }
}
