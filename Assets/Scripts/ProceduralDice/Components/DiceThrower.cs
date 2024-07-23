using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DiceThrower : MonoBehaviour
{
    [SerializeField]
    private int _seed;

    [SerializeField]
    private Rigidbody _diePhysics;
    [SerializeField]
    private ProceduralDie _die;

    [SerializeField]
    private float _forceModifier;
    [SerializeField]
    private float _torqueModifier;

    public void ResetState()
    {
        UnityEngine.Random.InitState(_seed);
    }

    private void Awake()
    {
        ResetState();
        // OnRollFinished += (side) => Debug.Log($"Die stopped, topmost side: {side}");
    }

    [SerializeField]
    private Vector3 _defaultPosition;

    public void Roll()
    {
        if (_watching)
        {
            Debug.Log("Die is already rolling");
            return; 
        }

        _die.transform.position = _defaultPosition;

        _diePhysics.AddRelativeTorque(UnityEngine.Random.insideUnitSphere * _torqueModifier, ForceMode.Impulse);
        _diePhysics.AddRelativeForce(UnityEngine.Random.insideUnitSphere * _forceModifier, ForceMode.Impulse);

        _lastKnownPosition = _diePhysics.position + UnityEngine.Random.insideUnitSphere;
        _watching = true;
    }

    private bool _watching;
    private Vector3 _lastKnownPosition;

    [SerializeField]
    private Vector3 _normal = Vector3.up;

    public delegate void OnRollFinishedEvent(int side);
    public OnRollFinishedEvent OnRollFinished;

    private void ProcessRollResult()
    {
        _watching = false;

        int selectedSide = _die.GetRolledSide(_normal);
        OnRollFinished?.Invoke(selectedSide);
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

[CustomEditor(typeof(DiceThrower))]
public class DiceThrowerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var diceThrower = target as DiceThrower;

        EditorGUILayout.Space(20f);

        if (GUILayout.Button("Throw") && Application.isPlaying)
        {
            diceThrower.Roll();
        }

        if (GUILayout.Button("Reset state") && Application.isPlaying)
        {
            diceThrower.ResetState();
        }
    }
}