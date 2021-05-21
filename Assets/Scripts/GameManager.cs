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
/// <summary>
/// Handles the flow of entire app.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    /// <summary>
    /// GLTF type file importer.
    /// </summary>
    /// <value>
    /// Holds the object of GLTFImpoerterUpdated class
    /// </value>
    [SerializeField]
    private GLTFImporterUpdated gltfImporter;

    /// <summary>
    /// Invoked when the  "Viewer" scene is loaded/unloaded
    /// </summary>
    public event Action OnGameSceneChanged;

    /// <summary>
    /// Used to determine current active GameScene
    /// </summary>
    /// <value>
    /// Home = Default Scene when app is first opened
    /// Viewer = Scene loaded after object import, Used to display AR scene
    /// Editor = Not yet used
    /// </value>
    public enum GameScene
    {
        Home,
        Viewer,
        Editor
    }

    /// <summary>
    /// Store constants for file types
    /// </summary>
    const string TypeOBJ = ".obj";
    const string TypeGLTF = ".gltf";
    const string TypeGLB = ".glb";

    /// <summary>
    /// Stores the name of loaded file
    /// </summary>
    private string _loadFileName = default;

    /// <summary>
    /// Current Game Scene
    /// </summary>
    /// <value>
    /// Holds current Game Scene
    /// </value>
    GameScene _currentGameScene = GameScene.Home;
    
    /// <summary>
    /// Holds Previous Game Scene
    /// </summary>
    GameScene _previousGameScene;

    /// <summary>
    /// Imported Object
    /// </summary>
    GameObject _viewerObject;

    /// <summary>
    /// Game Object that display a message while loading the imported object and starting the Viewer Scene
    /// </summary>
    GameObject _loadingMessage;

    /// <summary>
    /// Property for _currentGameScene
    /// </summary>
    public GameScene CurrentGameScene
    {
        get { return _currentGameScene; }
        set { _currentGameScene = value; }
    }


    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
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

    /// <summary>
    /// Unity's Async Op used to Load Viewer Scene
    /// </summary>
    /// <remarks>
    /// Converted to Async op from normal method so that the app can wait(using isDone property of Async Op) and display the loading message while the Viewer Scene is loaded
    /// </remarks>
    /// <param name="gameScene">Loads this scene</param>
    /// <returns>Async Op object that has "isDone" property with which we can wait until the Operation is completed</returns>
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

    /// <summary>
    /// Unloads the given gameScene (Viewer)
    /// </summary>
    /// <param name="gameScene">GameScene to be unloaded</param>
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
    
    /// <summary>
    /// Opens the File Browser to select the file to be imported
    /// </summary>
    /// <returns>IEnumerator object</returns>
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

    /// <summary>
    /// Opens the File Browser selected file/Folder will be uploaded to AmazonS3
    /// </summary>
    /// <returns>IEnumerator object</returns>
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

    /// <summary>
    /// Displays File Browser to select a file Location to save the exported glTF file
    /// </summary>
    /// <returns>IEnumerator Object</returns>
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

    /// <summary>
    /// Used for creating the object of GLTFSceneExporter class
    /// </summary>
    /// <param name="texture">Takes texture as input</param>
    /// <returns>Name of texture</returns>
    private string RetrieveTexturePath(Texture texture)
    {
        return texture.name;
    }

    /// <summary>
    /// Displays a pop-up box while selected file is getting imported and the Viewer Scene is loaded
    /// </summary>
    void DisplayLoadingMessage()
    {
        _loadingMessage = UIManager.Instance.CreateMessageWindow();
        if (_loadingMessage != null)
        {
            MessageFields msgFields = _loadingMessage.GetComponent<MessageFields>();
            msgFields.MessageDetails("Loading...", "Importing selected object and starting AR Scene.");
        }
    }

    /// <summary>
    /// Determines the type of file selected by user and imports the selected object
    /// </summary>
    /// <param name="sourcePath">Path of object file to be imported</param>
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

    /// <summary>
    /// After the object is imported into Unity it is passed to Viewer scene to display the object in AR
    /// </summary>
    /// <returns>IEnumerator Object</returns>
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

    /// <summary>
    /// Async Task that loads the selected 3D object into Unity
    /// </summary>
    async void GLTFLoaderTask()
    {
        if (gltfImporter.GLTFUri != null)
            await gltfImporter.Load();
    }

    /// <summary>
    /// Assigns the imported object from GLTFImporterUpdated to the global viewer object that is further passed on to Viewer Scene
    /// </summary>
    /// <param name="gltfObject"></param>
    public void GLTFObjectAssignment(GameObject gltfObject)
    {
        _viewerObject = gltfObject;
    }    

    /// <summary>
    /// Destroys the pop-up message displayed after the Viewer Scene is loaded
    /// </summary>
    public void DestroyLoadingMessage()
    {
        if (_loadingMessage != null)
            Destroy(_loadingMessage);
    }

    /// <summary>
    /// Error message shown as pop-up if the selected file type is not supported for importing
    /// </summary>
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
