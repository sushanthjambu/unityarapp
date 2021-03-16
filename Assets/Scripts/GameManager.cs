using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleFileBrowser;
using Dummiesman;

public class GameManager : Singleton<GameManager>
{
    public event Action OnGameSceneChanged;
    public enum GameScene
    {
        Home,
        Viewer,
        Editor
    }

    GameScene _currentGameScene = GameScene.Home;
    GameScene _previousGameScene;

    GameObject _viewerObject;

    public GameScene CurrentGameScene
    {
        get { return _currentGameScene; }
        set { _currentGameScene = value; }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            if (_currentGameScene == GameScene.Home)
            {
                Application.Quit();
            }
            else
            {
                UnloadLevel(_currentGameScene);
            }
        }
    }

    public AsyncOperation LoadLevel(GameScene gameScene)
    {
        if (!SceneManager.GetSceneByName(gameScene.ToString()).isLoaded)
        {
            AsyncOperation ao = SceneManager.LoadSceneAsync(gameScene.ToString(), LoadSceneMode.Additive);
            if (ao == null)
            {
                Debug.LogError("[Scene Manager] Unable to load Level " + gameScene.ToString());
                return null;
            }
            
            if (_currentGameScene != gameScene)
            {
                _previousGameScene = _currentGameScene;
            }
            _currentGameScene = gameScene;

            if (OnGameSceneChanged != null)
            {
                OnGameSceneChanged();
            }
            return ao;
        }
        return null;
    }

    public void UnloadLevel(GameScene gameScene)
    {
        if (SceneManager.GetSceneByName(gameScene.ToString()).isLoaded)
        {
            AsyncOperation ao = SceneManager.UnloadSceneAsync(gameScene.ToString());
            if (ao == null)
            {
                Debug.LogError("[Scene Manager] Unable to unload Level " + gameScene.ToString());
                return;
            }

            if (gameScene != GameScene.Home)
            {
                _currentGameScene = _previousGameScene;
            }

            if (OnGameSceneChanged != null)
            {
                OnGameSceneChanged();
            }
        }
    }
    
    public IEnumerator DisplayLoadCoroutine()
    {
        FileBrowser.SingleClickMode = true;

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Import file", "Load");

        if (FileBrowser.Success)
        {
            Debug.Log(FileBrowser.Result[0]);
            LoadObjfromFile(FileBrowser.Result[0]);
        }
    }

    void LoadObjfromFile(string sourcePath)
    {
        _viewerObject = new OBJLoader().Load(sourcePath);
        if (_viewerObject != null)
        {
            Debug.Log("Object loaded successfully");
            StartCoroutine(InstantiateLoadedObject());
        }
    }

    IEnumerator InstantiateLoadedObject()
    {
        AsyncOperation asyncOp = LoadLevel(GameScene.Viewer);

        while (!asyncOp.isDone)
            yield return null;

        yield return new WaitForEndOfFrame();

        if (_currentGameScene == GameScene.Viewer && SceneManager.GetSceneByName(GameScene.Viewer.ToString()).isLoaded)
        {
            Debug.Log("Active Scene is : " + SceneManager.GetActiveScene().name);
            if (SceneManager.GetActiveScene().name != GameScene.Viewer.ToString())
            {
                Debug.Log("Setting Viewer as Active Scene");
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(GameScene.Viewer.ToString()));
            }
            Debug.Log("Active Scene is : " + SceneManager.GetActiveScene().name);
            Instantiate(_viewerObject, Vector3.zero, Quaternion.identity);
        }
    }

}
