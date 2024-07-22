using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DieRandomRotate : MonoBehaviour
{
    [SerializeField]
    private int _seed;

    [SerializeField]
    private DiceRotatorBase _rotator;

    private void Awake()
    {
        UnityEngine.Random.InitState(_seed);
    }

    public void RandomRotate(float duration)
    {
        var side = UnityEngine.Random.Range(1, _rotator.SidesCount + 1);
        _rotator.RotateDieAnimated(side, duration);
    }
}
