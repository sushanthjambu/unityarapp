using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GameState
    {
        Home,
        Viewer,
        Editor
    }

    GameState _currentGameState = GameState.Home;

    public GameState CurrentGameState
    {
        get { return _currentGameState; }
        set { _currentGameState = value; }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void LoadLevel(GameState gameState)
    {
        AsyncOperation ao = SceneManager.LoadSceneAsync(gameState.ToString(), LoadSceneMode.Additive);
        if (ao == null)
        {
            Debug.LogError("[Scene Manager] Unable to load Level " + gameState.ToString());
            return;
        }
    }

    public void UnloadLevel(GameState gameState)
    {
        AsyncOperation ao = SceneManager.UnloadSceneAsync(gameState.ToString());
        if (ao == null)
        {
            Debug.LogError("[Scene Manager] Unable to unload Level " + gameState.ToString());
            return;
        }
    }

}
