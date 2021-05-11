using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using SimpleFileBrowser;
using Dummiesman;
using UnityGLTF;
using Unity.SharpZipLib.Utils;

public class GameManager : Singleton<GameManager>
{
    [SerializeField]
    private GLTFImporterUpdated gltfImporter;

    public event Action OnGameSceneChanged;
    public enum GameScene
    {
        Home,
        Viewer,
        Editor
    }

    const string TypeOBJ = ".obj";
    const string TypeGLTF = ".gltf";
    const string TypeGLB = ".glb";

    private string _loadFileName = default;

    GameScene _currentGameScene = GameScene.Home;
    GameScene _previousGameScene;

    GameObject _viewerObject;

    GameObject _loadingMessage;

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
                if (UIManager.Instance.IsOptionsOpen)
                {
                    UIManager.Instance.OnOptionsClick();
                }
                else
                {
                    Application.Quit();
                }
            }
            else
            {
                Destroy(_viewerObject);
                _viewerObject = null;
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
            DisplayLoadingMessage();
            Debug.Log(FileBrowser.Result[0]);
            LoadObjfromFile(FileBrowser.Result[0]);
        }
    }

    public IEnumerator DisplayWebARLoadCoroutine()
    {
        FileBrowser.SingleClickMode = true;

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, false, null, null, "Upload File/Folder", "Upload");

        if (FileBrowser.Success)
        {
            if (FileUploader.Instance.IsValidUpload(FileBrowser.Result[0], out string errorMessage))
            {
                FileUploader.Instance.UploadToAmazonS3(FileBrowser.Result[0]);
            }
            else
            {
                if (errorMessage != null)
                    FileUploader.Instance.DisplayUploadErrorMessage(errorMessage);
            }
        }
    }

    public IEnumerator DisplaySaveCoroutine()
    {
        FileBrowser.SingleClickMode = true;

        yield return FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.FilesAndFolders, false, null, null, "Select Folder", "Save");

        if (FileBrowser.Success)
        {
            string returnedPath = FileBrowser.Result[0];
            Debug.Log("Export Path : " + returnedPath);

            var exporter = new GLTFSceneExporter(new[] { _viewerObject.transform }, RetrieveTexturePath);
            if (FileBrowserHelpers.DirectoryExists(returnedPath))
            {
                string folderName = _loadFileName ?? "ExportedObject";
                string finalExportPath = FileBrowserHelpers.CreateFolderInDirectory(returnedPath, folderName);
                Debug.Log("Final Export Path for empty filename : " + finalExportPath);
                exporter.SaveGLTFandBin(finalExportPath, folderName);
            }
            else
            {
                string saveFileName = FileBrowserHelpers.GetFilename(returnedPath);
                string folderPath = returnedPath.Substring(0, returnedPath.LastIndexOf(saveFileName));
                string finalExportPath = FileBrowserHelpers.CreateFolderInDirectory(folderPath, saveFileName);
                Debug.Log("Final Export Path with given filename : " + finalExportPath);
                exporter.SaveGLTFandBin(finalExportPath, saveFileName);
            }
        }

    }

    private string RetrieveTexturePath(Texture texture)
    {
        return texture.name;
    }

    void DisplayLoadingMessage()
    {
        _loadingMessage = UIManager.Instance.CreateMessageWindow();
        if (_loadingMessage != null)
        {
            MessageFields msgFields = _loadingMessage.GetComponent<MessageFields>();
            msgFields.MessageDetails("Loading...", "Importing selected object and starting AR Scene.");
        }
    }

    void LoadObjfromFile(string sourcePath)
    {
        bool isInvalidFileType = false;
        string fileName = FileBrowserHelpers.GetFilename(sourcePath).ToLower();
        if (fileName.EndsWith(TypeOBJ))
        {
            _viewerObject = new OBJLoader().Load(sourcePath);
            _loadFileName = fileName.Substring(0, fileName.LastIndexOf(TypeOBJ));
        }
        else if (fileName.EndsWith(TypeGLTF))
        {
            gltfImporter.GLTFUri = sourcePath;
            GLTFLoaderTask();
            _loadFileName = fileName.Substring(0, fileName.LastIndexOf(TypeGLTF));
        }
        else if (fileName.EndsWith(TypeGLB))
        {
            gltfImporter.GLTFUri = sourcePath;
            GLTFLoaderTask();
            _loadFileName = fileName.Substring(0, fileName.LastIndexOf(TypeGLB));
        }
        else
        {
            isInvalidFileType = true;
            DestroyLoadingMessage();
            DisplayFileErrorMessage();
        }            

        if (!isInvalidFileType)
        {
            StartCoroutine(LoadedObjectToViewer());
        }
        //if (_viewerObject != null)
        //{
        //    _viewerObject.SetActive(false);
        //    Debug.Log("object loaded successfully");
        //    StartCoroutine(LoadedObjectToViewer());
        //}
    }

    IEnumerator LoadedObjectToViewer()
    {
        while (_viewerObject == null)
            yield return null;
        yield return new WaitForEndOfFrame();

        _viewerObject.SetActive(false);

        AsyncOperation asyncOp = LoadLevel(GameScene.Viewer);

        while (!asyncOp.isDone)
            yield return null;

        yield return new WaitForEndOfFrame();
        DestroyLoadingMessage();

        if (_currentGameScene == GameScene.Viewer && SceneManager.GetSceneByName(GameScene.Viewer.ToString()).isLoaded)
        {
            //Debug.Log("Active Scene is : " + SceneManager.GetActiveScene().name);
            if (SceneManager.GetActiveScene().name != GameScene.Viewer.ToString())
            {
                //Debug.Log("Setting Viewer as Active Scene");
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(GameScene.Viewer.ToString()));
            }
            //Debug.Log("Active Scene is : " + SceneManager.GetActiveScene().name);
            ARViewManager.Instance.AssignObject(_viewerObject);
        }
    }

    async void GLTFLoaderTask()
    {
        if (gltfImporter.GLTFUri != null)
            await gltfImporter.Load();
    }

    public void GLTFObjectAssignment(GameObject gltfObject)
    {
        _viewerObject = gltfObject;
    }    

    public void DestroyLoadingMessage()
    {
        if (_loadingMessage != null)
            Destroy(_loadingMessage);
    }

    void DisplayFileErrorMessage()
    {
        GameObject fileErrorMessage = UIManager.Instance.CreateMessageWindow();
        if (fileErrorMessage != null)
        {
            MessageFields msgFields = fileErrorMessage.GetComponent<MessageFields>();
            msgFields.MessageDetails("File Type Error!", "Selected File type cannot be loaded, please select \".obj\" or \".gltf\" files.", "OK");
            Transform okTrans = fileErrorMessage.transform.Find("Done");
            if (okTrans != null)
            {
                Button okButton = okTrans.gameObject.GetComponent<Button>();
                okButton.onClick.AddListener(() => { Destroy(fileErrorMessage); });
            }
        }
    }    

}
