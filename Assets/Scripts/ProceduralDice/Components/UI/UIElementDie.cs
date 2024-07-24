using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIElementDie : VisualElement, IDiceRotator
{
    private int _seed;
    private int _defaultSide;
    private UIDieData _dieData;

    [SerializeField]
    private bool _explicitNumbers;
    public bool ExplicitNumbers
    {
        get
        {
            return _explicitNumbers;
        }
        set
        {
            _explicitNumbers = value;
        }
    }

    private Label _numbersLabel;

    public int SidesCount => _dieData.SidesTextures.Count;

    public void Initialize(UIDieData dieData, int defaultSide = 1, int seed = 0)
    {
        _seed = seed;
        _defaultSide = defaultSide;
        _dieData = dieData;

        _numbersLabel = new Label();
        Add(_numbersLabel);

        _numbersLabel.style.color = Color.white;
        _numbersLabel.style.unityTextOutlineColor = Color.black;
        _numbersLabel.style.unityTextOutlineWidth = 2f;

        _numbersLabel.style.fontSize = 144;
        _numbersLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        _numbersLabel.text = string.Empty;

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
        style.justifyContent = Justify.Center;
        if (_explicitNumbers)
        {
            _numbersLabel.text = side.ToString();
        }
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

            if (_explicitNumbers)
            {
                _numbersLabel.text = UnityEngine.Random.Range(1, SidesCount + 1).ToString();
            }

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
