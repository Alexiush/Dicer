using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceRotator : MonoBehaviour
{
    [SerializeField]
    private ProceduralDice _die;
    [SerializeField]
    private Vector3 _topDirection;
    [SerializeField] 
    private Vector3 _forwardDirection;

    [SerializeField]
    private int _defaultSide;

    private void Start()
    {
        _die.OnNewGeneration += mesh => RotateDie(_defaultSide);
    }

    public void RotateDie(int side)
    {
        _die.SideRotation(side - 1, _topDirection, _forwardDirection);
    }
}
