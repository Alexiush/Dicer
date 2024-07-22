using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIElementDie : VisualElement, IDiceRotator
{
    private int _seed;
    private int _defaultSide;
    private UIDieData _dieData;

    public int SidesCount => _dieData.SidesTextures.Count;

    public void Initialize(UIDieData dieData, int defaultSide = 1, int seed = 0)
    {
        _seed = seed;
        _defaultSide = defaultSide;
        _dieData = dieData;

        UnityEngine.Random.InitState(_seed);
        RotateDie(_defaultSide);
    }

    public new class UxmlFactory : UxmlFactory<UIElementDie> { }

    public UIElementDie() { }

    public void RotateDie(int side)
    {
        if (_rotating)
        {
            Debug.Log("Already rotating");
            return;
        }

        style.backgroundImage = new StyleBackground(_dieData.SidesTextures[side - 1]);
    }

    private bool _rotating;
    private float _timeLeft;
    private int _frame;
    private int _processedTargetSide;

    private void RotationRoutine()
    {
        if (!_rotating)
        {
            return;
        }

        _timeLeft -= Time.deltaTime;

        if (_timeLeft > 0)
        {
            style.backgroundImage = new StyleBackground(_dieData.AnimationFrames[_frame % _dieData.AnimationFrames.Count]);
            _frame++;

            this.schedule.Execute(() => RotationRoutine());
        }
        else
        {
            _rotating = false;
            RotateDie(_processedTargetSide);
        }
    }

    public void RotateDieAnimated(int side, float animationDuration)
    {
        if (_rotating)
        {
            Debug.Log("Already rotating");
            return;
        }

        _frame = 0;
        _timeLeft = animationDuration;
        _rotating = true;
        _processedTargetSide = side;

        this.schedule.Execute(() => RotationRoutine());
    }
}
