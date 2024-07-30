using System.Collections.Generic;
using UnityEngine;

namespace Dicer.UI
{
    [CreateAssetMenu(fileName = "UIDieData", menuName = "Dicer/UIDie/UIDieData")]
    public class UIDieData : ScriptableObject
    {
        public List<Sprite> SidesTextures;
        public List<Sprite> AnimationFrames;
    }
}
