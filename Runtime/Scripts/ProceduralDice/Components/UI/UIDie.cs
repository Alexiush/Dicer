using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.U2D.Sprites;
using TMPro;
using Dicer.Components;

namespace Dicer.UI
{
    [RequireComponent(typeof(Image))]
    public class UIDie : DiceRotatorBase
    {
        public UIDieData dieData;
        private Image _die;

        public override int SidesCount => dieData.SidesTextures.Count;

        [SerializeField]
        private int _seed;
        [SerializeField]
        private int _defaultSide;

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
                _numbersLabel.enabled = _explicitNumbers;
            }
        }

        [SerializeField]
        private TextMeshProUGUI _numbersLabel;

        private void Awake()
        {
            UnityEngine.Random.InitState(_seed);
            _die = GetComponent<Image>();

            _numbersLabel.text = string.Empty;
        }

        private void Start()
        {
            RotateDie(_defaultSide);
        }

        public override void RotateDie(int side)
        {
            if (_rotating)
            {
                Debug.Log("Already rotating");
                return;
            }

            _die.sprite = dieData.SidesTextures[side - 1];

            if (_explicitNumbers)
            {
                _numbersLabel.text = side.ToString();
            }
        }

        private bool _rotating;
        private float _timeLeft;
        private int _frame;
        private int _processedTargetSide;

        public override void RotateDieAnimated(int side, float animationDuration)
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
                _die.sprite = dieData.AnimationFrames[_frame % dieData.AnimationFrames.Count];

                if (_explicitNumbers)
                {
                    _numbersLabel.text = UnityEngine.Random.Range(1, SidesCount + 1).ToString();
                }

                _frame++;
            }
            else
            {
                _rotating = false;
                RotateDie(_processedTargetSide);
            }
        }
    }
}
