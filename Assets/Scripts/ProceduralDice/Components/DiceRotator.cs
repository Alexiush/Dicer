using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class DiceRotator : MonoBehaviour
{
    [SerializeField]
    private int _seed;

    [SerializeField]
    private ProceduralDice _die;
    
    [SerializeField]
    private Vector3 _topDirection;
    [SerializeField] 
    private Vector3 _forwardDirection;

    [SerializeField]
    private int _defaultSide;

    private void Awake()
    {
        UnityEngine.Random.InitState(_seed);
    }

    private void Start()
    {
        _die.OnNewGeneration += mesh => RotateDie(_defaultSide);
    }

    public void RotateDie(int side)
    {
        _die.transform.rotation = Quaternion.identity;
        _die.SideRotation(side - 1, _topDirection, _forwardDirection);
    }

    private bool _rotating;
    private float _timeLeft;
    private int _processedTargetSide;

    public int TargetSide { get; set; } = 1;

    public void RotateDieAnimated(int side, float animationDuration)
    {
        if (_rotating)
        {
            Debug.Log("Already rotating");
            return;
        }

        _timeLeft = animationDuration;
        _rotating = true;
        _processedTargetSide = side;
    }

    private void Update()
    {
        if (!_rotating)
        {
            return;
        }

        _timeLeft -= Time.deltaTime;

        if (_timeLeft > 0)
        {
            _die.transform.Rotate(UnityEngine.Random.insideUnitSphere * 180);
        }
        else
        {
            _rotating = false;
            RotateDie(_processedTargetSide);
        }
    }
}

[CustomEditor(typeof(DiceRotator))]
public class DiceRotatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var diceRotator = target as DiceRotator;

        diceRotator.TargetSide = EditorGUILayout.IntField(diceRotator.TargetSide);
        if (GUILayout.Button("Rotate"))
        {
            diceRotator.RotateDieAnimated(diceRotator.TargetSide, 5);
        }
    }
}
