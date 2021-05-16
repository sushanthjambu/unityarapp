using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the UI of app
/// </summary>
public class UIManager : Singleton<UIManager>
{
    /// <summary>
    /// Home screen UI Object
    /// </summary>
    [SerializeField] GameObject _homeScreen;

    /// <summary>
    /// Slide screen used for options menu on top left
    /// </summary>
    [SerializeField] GameObject _optionsScreen;

    /// <summary>
    /// Pop-up message Prefab(MessageWindow)
    /// </summary>
    [SerializeField] GameObject _messageWindow;

    /// <summary>
    /// Rect Transform of Slide Screen/options Menu
    /// </summary>
    RectTransform _optionsRectTransform;

    /// <summary>
    /// Tells if the slide screen is opened
    /// </summary>
    bool _isOptionsOpen = false;

    /// <summary>
    /// Property of _isOptionsOpen
    /// </summary>
    public bool IsOptionsOpen
    {
        get { return _isOptionsOpen; }
    }

    /// <summary>
    /// Position of Options Screen when closed. Initialized in Start()
    /// </summary>
    Vector2 _closedPosition = Vector2.zero;

    /// <summary>
    /// Position of Options Screen when open. Initialized in Start()
    /// </summary>
    Vector2 _openPosition = Vector2.zero;

    /// <summary>
    /// Time taken for Options screen to be opened completely
    /// </summary>
    float _slideDuration = 0.5f;
    void Start()
    {
        GameManager.Instance.OnGameSceneChanged += ToggleHomeScreen;
        _optionsRectTransform = _optionsScreen.GetComponent<RectTransform>();
        _closedPosition = _optionsRectTransform.anchoredPosition;
        _openPosition = _closedPosition;
        _openPosition.x += _optionsRectTransform.rect.width;
    }

    /// <summary>
    /// If User clicks the Options Button on the top left of Home Screen
    /// </summary>
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

    /// <summary>
    /// Moves the Options Screen to the given Vector2 Position. Used to slide the options screen
    /// </summary>
    /// <param name="moveToPosition">Position to which the screen is to be moved</param>
    /// <returns></returns>
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

    /// <summary>
    /// Triggered when the Viewer Scene is loaded, turns off the home screen UI and vice-versa
    /// </summary>
    void ToggleHomeScreen()
    {
        _homeScreen.SetActive(GameManager.Instance.CurrentGameScene == GameManager.GameScene.Home);
        _optionsScreen.SetActive(GameManager.Instance.CurrentGameScene == GameManager.GameScene.Home);
    }

    /// <summary>
    /// If User clicks the Browse button on the center
    /// </summary>
    public void onBrowse()
    {
        StartCoroutine(GameManager.Instance.DisplayLoadCoroutine());
    }

    /// <summary>
    /// Created a Pup-up message window object in the scene using the _messageWindow prefab
    /// </summary>
    /// <returns>The created message window</returns>
    public GameObject CreateMessageWindow()
    {
        GameObject dynamicCanvas = GameObject.Find("DynamicCanvas");
        if (dynamicCanvas != null)
            return (Instantiate(_messageWindow, dynamicCanvas.transform));
        return null;
    }

    /// <summary>
    /// If user clicks the Exit button in the Options screen
    /// </summary>
    public void OnOptionExit()
    {
        Application.Quit();
    }

    /// <summary>
    /// If user clicks the Viewer button in the Options screen
    /// </summary>
    public void OnOptionViewer()
    {
        GameObject viewerWindow = CreateMessageWindow();
        if (viewerWindow != null)
        {
            MessageFields msgFields = viewerWindow.GetComponent<MessageFields>();
            msgFields.MessageDetails("AR Viewer", "Click on \"Browse\" and select a 3D file to view in AR", "Browse", "Cancel");
            Transform browseTrans = viewerWindow.transform.Find("Done");
            if (browseTrans != null)
            {
                Button browse = browseTrans.gameObject.GetComponent<Button>();
                browse.onClick.AddListener(() => { Destroy(viewerWindow); StartCoroutine(GameManager.Instance.DisplayLoadCoroutine()); });
            }

            Transform cancelTrans = viewerWindow.transform.Find("Cancel");
            if(cancelTrans != null)
            {
                Button cancel = cancelTrans.gameObject.GetComponent<Button>();
                cancel.onClick.AddListener(() => Destroy(viewerWindow));
            }
        }

    }

    /// <summary>
    /// If user clicks the WebAR button in the Options screen
    /// </summary>
    public void OnOptionWebAR()
    {
        GameObject webARWindow = CreateMessageWindow();
        if (webARWindow != null)
        {
            MessageFields msgFieds = webARWindow.GetComponent<MessageFields>();
            msgFieds.MessageDetails("Web AR", "Select a glTF/glb file or a Folder/Zip containing a gltf/glb file to generate Web URL\nNote : Selected File/Folder will be uploaded to remote server to generate the URL.", "Browse", "Cancel");
            Transform browseTrans = webARWindow.transform.Find("Done");
            if (browseTrans != null)
            {
                Button browse = browseTrans.gameObject.GetComponent<Button>();
                browse.onClick.AddListener(() => { Destroy(webARWindow); StartCoroutine(GameManager.Instance.DisplayWebARLoadCoroutine()); });
            }

            Transform cancelTrans = webARWindow.transform.Find("Cancel");
            if (cancelTrans != null)
            {
                Button cancel = cancelTrans.gameObject.GetComponent<Button>();
                cancel.onClick.AddListener(() => Destroy(webARWindow));
            }
        }
    }
}
