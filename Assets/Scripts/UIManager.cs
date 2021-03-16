using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] GameObject _homeScreen;
    void Start()
    {
        GameManager.Instance.OnGameSceneChanged += ToggleHomeScreen;
    }

    public void OnOptionsClick()
    {
        GameManager.Instance.LoadLevel(GameManager.GameScene.Viewer);
    }

    void ToggleHomeScreen()
    {
        _homeScreen.SetActive(GameManager.Instance.CurrentGameScene == GameManager.GameScene.Home);
    }

    public void onBrowse()
    {
        StartCoroutine(GameManager.Instance.DisplayLoadCoroutine());
    }
}
