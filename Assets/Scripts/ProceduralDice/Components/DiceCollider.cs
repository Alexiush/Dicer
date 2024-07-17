using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class DiceCollider : MonoBehaviour
{
    [SerializeField]
    private ProceduralDice _diceGenerator;
    private MeshCollider _collider; 

    private void OnEnable()
    {
        _collider = GetComponent<MeshCollider>();
        _diceGenerator.OnNewGeneration += mesh => _collider.sharedMesh = mesh;
    }
}
