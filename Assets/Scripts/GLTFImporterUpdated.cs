using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using GLTF;
using GLTF.Schema;
using UnityEngine;
using UnityEngine.UI;
using UnityGLTF.Loader;

namespace UnityGLTF
{
	/// <summary>
	/// Handles the import of GLTF object only
	/// </summary>
	public class GLTFImporterUpdated : MonoBehaviour
	{
		/// <summary>
		/// Location of the gltf object to be imported
		/// </summary>
		public string GLTFUri = null;

		/// <summary>
		/// If true import operation becomes a multithreaded process
		/// </summary>
		public bool Multithreaded = true;

		/// <summary>
		/// Set true if object is on local machine
		/// </summary>
		public bool UseStream = true;

		/// <summary>
		/// If object is in AppendStreaming Assets folder
		/// </summary>
		public bool AppendStreamingAssets = false;

		/// <summary>
		/// Set true if you also want to play the animation of imported gltf object
		/// </summary>
		public bool PlayAnimationOnLoad = true;
		
		[SerializeField]
		private bool loadOnStart = false;

		[SerializeField] private bool MaterialsOnly = false;

		[SerializeField] private int RetryCount = 10;
		[SerializeField] private float RetryTimeout = 2.0f;
		private int numRetries = 0;


		public int MaximumLod = 300;
		public int Timeout = 8;
		public GLTFSceneImporter.ColliderType Collider = GLTFSceneImporter.ColliderType.None;

		private AsyncCoroutineHelper asyncCoroutineHelper;

		[SerializeField]
		private Shader shaderOverride = null;

		private async void Start()
		{
			if (!loadOnStart) return;

			try
			{
				await Load();
			}
#if WINDOWS_UWP
			catch (Exception)
#else
			catch (HttpRequestException)
#endif
			{
				if (numRetries++ >= RetryCount)
					throw;

				Debug.LogWarning("Load failed, retrying");
				await Task.Delay((int)(RetryTimeout * 1000));
				Start();
			}
		}

		/// <summary>
		/// Actual asynce task that loads the gltf object into Unity
		/// </summary>
		/// <returns></returns>
		public async Task Load()
		{
			asyncCoroutineHelper = gameObject.GetComponent<AsyncCoroutineHelper>() ?? gameObject.AddComponent<AsyncCoroutineHelper>();
			GLTFSceneImporter sceneImporter = null;
			ILoader loader = null;
			try
			{
				if (UseStream)
				{
					// Path.Combine treats paths that start with the separator character
					// as absolute paths, ignoring the first path passed in. This removes
					// that character to properly handle a filename written with it.
					GLTFUri = GLTFUri.TrimStart(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
					string fullPath;
					if (AppendStreamingAssets)
					{
						fullPath = Path.Combine(Application.streamingAssetsPath, GLTFUri);
					}
					else
					{
						fullPath = GLTFUri;
					}
					string directoryPath = URIHelper.GetDirectoryName(fullPath);
					loader = new FileLoader(directoryPath);
					sceneImporter = new GLTFSceneImporter(
						Path.GetFileName(GLTFUri),
						directoryPath,
						loader,
						asyncCoroutineHelper
						);
				}
				else
				{
					string directoryPath = URIHelper.GetDirectoryName(GLTFUri);
					loader = new WebRequestLoader(directoryPath);

					sceneImporter = new GLTFSceneImporter(
						URIHelper.GetFileFromUri(new Uri(GLTFUri)),
						loader,
						asyncCoroutineHelper
						);

				}

				sceneImporter.SceneParent = gameObject.transform;
				sceneImporter.Collider = Collider;
				sceneImporter.MaximumLod = MaximumLod;
				sceneImporter.Timeout = Timeout;
				sceneImporter.IsMultithreaded = Multithreaded;
				sceneImporter.CustomShaderName = shaderOverride ? shaderOverride.name : null;

				if (MaterialsOnly)
				{
					var mat = await sceneImporter.LoadMaterialAsync(0);
					var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
					cube.transform.SetParent(gameObject.transform);
					var renderer = cube.GetComponent<Renderer>();
					renderer.sharedMaterial = mat;
				}
				else
				{
					await sceneImporter.LoadSceneAsync(showSceneObj: false, onLoadComplete: GLTFObjectToGameManager);
				}

				// Override the shaders on all materials if a shader is provided
				if (shaderOverride != null)
				{
					Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
					foreach (Renderer renderer in renderers)
					{
						renderer.sharedMaterial.shader = shaderOverride;
					}
				}

				if (PlayAnimationOnLoad)
				{
					Animation[] animations = sceneImporter.LastLoadedScene.GetComponents<Animation>();
					foreach (Animation anim in animations)
					{
						anim.Play();
					}
				}
			}
			finally
			{
				if (loader != null)
				{
					sceneImporter?.Dispose();
					sceneImporter = null;
					loader = null;
				}
			}
		}

		/// <summary>
		/// Passes the loaded object to Game Manager
		/// </summary>
		/// <param name="gltfObject">Loaded gltf object that is to be passed</param>
		/// <param name="ex">Exception that occured while loading the gltf object</param>
		private void GLTFObjectToGameManager(GameObject gltfObject, ExceptionDispatchInfo ex)
		{
			if (gltfObject != null)
			{
				Debug.Log("GLTF object is imported.");
				GameManager.Instance.GLTFObjectAssignment(gltfObject);
			}
            else
            {
				Debug.LogError("[GLTF Import Error] : " + ex.SourceException.Message);
				GameManager.Instance.DestroyLoadingMessage();
				GLTFImportErrorMessage(ex.SourceException.Message);
            }
		}

		/// <summary>
		/// Displays the  error that occurred while importing the gltf object
		/// </summary>
		/// <param name="msg">Error Message to be displyed</param>
		private void GLTFImportErrorMessage(string msg)
        {
			GameObject GLTFImportErrorMessage = UIManager.Instance.CreateMessageWindow();
			if (GLTFImportErrorMessage != null)
			{
				MessageFields msgFields = GLTFImportErrorMessage.GetComponent<MessageFields>();
				msgFields.MessageDetails("GLTF Import Error!", msg, "OK");
				Transform okTrans = GLTFImportErrorMessage.transform.Find("Done");
				if (okTrans != null)
				{
					Button okButton = okTrans.gameObject.GetComponent<Button>();
					okButton.onClick.AddListener(() => { Destroy(GLTFImportErrorMessage); });
				}
			}
		}
	}
}

