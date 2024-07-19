using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DieRandomRotate : MonoBehaviour
{
    [SerializeField]
    private int _seed;

    [SerializeField]
    private ProceduralDie _die;
    [SerializeField]
    private DiceRotator _rotator;

    private void Awake()
    {
        UnityEngine.Random.InitState(_seed);
    }

    public void RandomRotate()
    {
        var side = UnityEngine.Random.Range(1, _die.DieSize + 1);
        _rotator.RotateDieAnimated(side, 3f);
    }
}
