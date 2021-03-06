using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public enum GameState
    {
        HOME,
        VIEWER,
        EDITOR
    }

    private string _currentScene = string.Empty;

    GameState _currentGameState = GameState.HOME;

    public GameState CurrentGameState
    {
        get { return _currentGameState; }
        set { _currentGameState = value; }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

}
