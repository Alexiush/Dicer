using UnityEngine;

namespace Dicer.Components
{
    [RequireComponent(typeof(MeshCollider))]
    public class DiceCollider : MonoBehaviour
    {
        [SerializeField]
        private ProceduralDie _diceGenerator;
        private MeshCollider _collider;

        private void OnEnable()
        {
            _collider = GetComponent<MeshCollider>();
            _diceGenerator.OnNewGeneration += mesh => _collider.sharedMesh = mesh;
        }
    }
}
