using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] GameObject _homeScreen;

    [SerializeField] GameObject _optionsScreen;

    [SerializeField] GameObject _messageWindow;

    RectTransform _optionsRectTransform;

    bool _isOptionsOpen = false;

    public bool IsOptionsOpen
    {
        get { return _isOptionsOpen; }
    }

    Vector2 _closedPosition = Vector2.zero;

    Vector2 _openPosition = Vector2.zero;

    float _slideDuration = 0.5f;
    void Start()
    {
        GameManager.Instance.OnGameSceneChanged += ToggleHomeScreen;
        _optionsRectTransform = _optionsScreen.GetComponent<RectTransform>();
        _closedPosition = _optionsRectTransform.anchoredPosition;
        _openPosition = _closedPosition;
        _openPosition.x += _optionsRectTransform.rect.width;
    }

    public void OnOptionsClick()
    {
        if (_isOptionsOpen)
        {
            //Debug.Log("Anchored Position : " + _optionsRectTransform.anchoredPosition + " to closed position : " + _closedPosition);
            StartCoroutine(LerpOptionsMenu(_closedPosition));
        }
        else
        {
            //Debug.Log("Anchored Position : " + _optionsRectTransform.anchoredPosition + " to open position : " + _openPosition);
            StartCoroutine(LerpOptionsMenu(_openPosition));
        }
    }

    IEnumerator LerpOptionsMenu(Vector2 moveToPosition)
    {
        float timeElapsed = 0;
        Vector2 startPosition = _optionsRectTransform.anchoredPosition;

        while(timeElapsed < _slideDuration)
        {
            _optionsRectTransform.anchoredPosition = Vector2.Lerp(startPosition, moveToPosition, timeElapsed / _slideDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        _optionsRectTransform.anchoredPosition = moveToPosition;
        _isOptionsOpen = !_isOptionsOpen;
    }

    void ToggleHomeScreen()
    {
        _homeScreen.SetActive(GameManager.Instance.CurrentGameScene == GameManager.GameScene.Home);
        _optionsScreen.SetActive(GameManager.Instance.CurrentGameScene == GameManager.GameScene.Home);
    }

    public void onBrowse()
    {
        StartCoroutine(GameManager.Instance.DisplayLoadCoroutine());
    }

    public GameObject CreateMessageWindow()
    {
        GameObject dynamicCanvas = GameObject.Find("DynamicCanvas");
        if (dynamicCanvas != null)
            return (Instantiate(_messageWindow, dynamicCanvas.transform));
        return null;
    }
}
