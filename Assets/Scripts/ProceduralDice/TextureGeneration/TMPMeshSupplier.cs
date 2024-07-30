using TMPro;
using UnityEngine;

namespace Dicer.TextureGeneration
{
    public class TMPMeshSupplier : MeshSupplier
    {
        [SerializeField]
        private TextMeshPro _tmp;

        public override Mesh GetMesh(int index)
        {
            var initialFontSize = _tmp.fontSize;

            _tmp.text = (index + 1).ToString();
            _tmp.fontSize = initialFontSize / _tmp.text.Length;
            _tmp.ForceMeshUpdate(true, true);

            var mesh = _tmp.mesh;

            _tmp.text = string.Empty;
            _tmp.fontSize = initialFontSize;

            return mesh;
        }
        public override Material Material => _tmp.fontSharedMaterial;
    }
}
