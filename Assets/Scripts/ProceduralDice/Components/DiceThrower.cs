using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DiceThrower : MonoBehaviour
{
    [SerializeField]
    private int _seed;

    [SerializeField]
    private Rigidbody _diePhysics;
    [SerializeField]
    private ProceduralDice _die;

    [SerializeField]
    private float _forceModifier;
    [SerializeField]
    private float _torqueModifier;

    private void Awake()
    {
        UnityEngine.Random.InitState(_seed);
    }

    private void Start()
    {
        // Dice should be thrown with some random force
        // However, just applying the force is not enough, it must rotate, etc

        _diePhysics.AddRelativeTorque(UnityEngine.Random.insideUnitSphere * _torqueModifier, ForceMode.Impulse);
        _diePhysics.AddRelativeForce(UnityEngine.Random.insideUnitSphere * _forceModifier, ForceMode.Impulse);

        // Start "watching" the die movements till it stops moving
        // When the movement stops - find the facing upwards triangle

        // As an update is called with the die position not changed yet it hits a false stop unless the flag is added or position is spoofed 
        _lastKnownPosition = _diePhysics.position + UnityEngine.Random.insideUnitSphere;
        _watching = true;
    }

    private bool _watching;
    private bool _dieStopped;
    private Vector3 _lastKnownPosition;

    [SerializeField]
    private Vector3 _normal = Vector3.up;

    private void ProcessRollResult()
    {
        _watching = false;
        _dieStopped = true;

        // Procedural die must provide functionality for detecting it's current topmost side

        int selectedSide = _die.GetRolledSide(_normal);
        Debug.Log($"Die stopped, topmost side: {selectedSide}");

        // Procedural die must provide functionality for getting the data associated with it's triangles
    }

    private void FixedUpdate()
    {
        if (_watching)
        {
            var currentPosition = _diePhysics.position;
            var delta = currentPosition - _lastKnownPosition;

            if (delta == Vector3.zero)
            {
                ProcessRollResult();
            }

            _lastKnownPosition = currentPosition;
        }
    }
}
