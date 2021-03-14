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
        Debug.Log("On Options Click");
    }

    void ToggleHomeScreen()
    {
        _homeScreen.SetActive(GameManager.Instance.CurrentGameScene == GameManager.GameScene.Home);
    }
}
