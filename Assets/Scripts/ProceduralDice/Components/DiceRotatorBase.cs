using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DiceRotatorBase : MonoBehaviour, IDiceRotator
{
    public abstract void RotateDie(int side);

    public abstract void RotateDieAnimated(int side, float animationDuration);

    public abstract int SidesCount { get; }
}

public interface IDiceRotator
{
    public void RotateDie(int side);

    public void RotateDieAnimated(int side, float animationDuration);

    public int SidesCount { get; }
}
