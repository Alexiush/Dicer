using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIToolkitDie : MonoBehaviour
{
    [SerializeField]
    private UIDocument _document;
    [SerializeField]
    private UIElementDie _die;
    [SerializeField]
    private UIDieData _dieData;
    [SerializeField]
    private Button _rotateButton;

    private void Start()
    {
        _die = _document.rootVisualElement.Q("UIDie") as UIElementDie;
        _die.Initialize(_dieData);

        _rotateButton = _document.rootVisualElement.Q("Rotate") as Button;
        _rotateButton.clicked += () => _die.RotateDieAnimated(UnityEngine.Random.Range(1, _die.SidesCount + 1), 1f);
    }
}
