using UnityEditor;
using UnityEngine;

namespace Dicer.Components
{
    public class DiceRotator : DiceRotatorBase
    {
        [SerializeField]
        private int _seed;

        [SerializeField]
        private ProceduralDie _die;
        public override int SidesCount => _die.DieSize;

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

        public override void RotateDie(int side)
        {
            if (_rotating)
            {
                Debug.Log("Already rotating");
                return;
            }

            _die.transform.rotation = Quaternion.identity;
            _die.SideRotation(side - 1, _topDirection, _forwardDirection);
        }

        private bool _rotating;
        private float _timeLeft;
        private int _processedTargetSide;

        public override void RotateDieAnimated(int side, float animationDuration)
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
        private int _targetSide = 1;
        private float _duration = 1f;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var diceRotator = target as DiceRotator;

            EditorGUILayout.Space(20f);

            _targetSide = EditorGUILayout.IntField("Target side", _targetSide);
            _duration = EditorGUILayout.FloatField("Duration", _duration);
            if (GUILayout.Button("Rotate") && Application.isPlaying)
            {
                diceRotator.RotateDieAnimated(_targetSide, _duration);
            }
        }
    }
}
