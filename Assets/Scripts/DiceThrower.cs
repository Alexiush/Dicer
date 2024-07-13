using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceThrower : MonoBehaviour
{
    [SerializeField]
    private Rigidbody _dicePhysics;
    [SerializeField]
    private float _forceModifier;

    private void Start()
    {
        // Dice should be thrown with some random force
        // However, just applying the force is not enough, it must rotate, etc
        _dicePhysics.AddForce(UnityEngine.Random.insideUnitSphere * _forceModifier, ForceMode.Impulse);
    }
}
