using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleFileBrowser;
using System.IO;
using Unity.SharpZipLib.Utils;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;

/// <summary>
/// Uploads the files to Remote Server/Amazon S3. Only for WebAR Feature
/// </summary>
public class FileUploader : Singleton<FileUploader>
{
    /// <summary>
    /// Url of your Backend Server. Change this to your server implementation. Do not let it be default if you want to use WebAR feature
    /// </summary>
    private string amazonS3ServerBackend = default;

    /// <summary>
    /// String Constants used to determine the file type
    /// </summary>
    const string TypeOBJ = ".obj";
    const string TypeGLTF = ".gltf";
    const string TypeGLB = ".glb";

    /// <summary>
    /// Max Upload File Size
    /// </summary>
    const long MaxUploadFileSize = 50 * 1023 * 1023;

    /// <summary>
    /// Stores the generated WebAR link
    /// </summary>
    string gltfUrl = default;

    /// <summary>
    /// Tells whether the user selected folder/file is valid i.e contains supported file format and checks file size.
    /// </summary>
    /// <param name="selectedPath">Path of file/folder user wants to upload</param>
    /// <param name="errorMessage">If any error pass error message</param>
    /// <returns>True if file/folder is valid else False</returns>
    public bool IsValidUpload(string selectedPath, out string errorMessage)
    {
        if (FileBrowserHelpers.DirectoryExists(selectedPath))
        {
            FileSystemEntry[] allFiles = FileBrowserHelpers.GetEntriesInDirectory(selectedPath);
            foreach (FileSystemEntry entry in allFiles)
            {
                if (!entry.IsDirectory)
                {
                    string uploadFileExtension = entry.Extension.ToLower();
                    if (uploadFileExtension == TypeGLTF || uploadFileExtension == TypeGLB)
                    {
                        errorMessage = null;
                        return true;
                    }
                }
            }
            errorMessage = "glTF/glb type file not found in selected folder.";
            return false;
        }
        else
        {
            string uploadFileName = FileBrowserHelpers.GetFilename(selectedPath).ToLower();
            if (uploadFileName.EndsWith(TypeGLTF) || uploadFileName.EndsWith(TypeGLB))
            {
                if (FileBrowserHelpers.GetFilesize(selectedPath) < MaxUploadFileSize)
                {
                    errorMessage = null;
                    return true;
                }
                else
                {
                    errorMessage = "File size is too large. It must be less than 50MB.";
                    return false;
                }
            }

            errorMessage = "File selected is not glTF or glb.";
            return false;
        }
    }

    /// <summary>
    /// Uploads file to server, if it is folder Zips the folder - Not used in case of upload to AmazonS3.
    /// Note - As the backend server supports only upload to AmazonS3 this is never called
    /// </summary>
    /// <param name="dataPath">Path of file/folder to be uploaded</param>
    public void UploadToServer(string dataPath)
    {
        if (FileBrowserHelpers.DirectoryExists(dataPath))
        {
            string zipPath = Path.Combine(Application.persistentDataPath, "Temp.zip");
            ZipUtility.CompressFolderToZip(zipPath, null, dataPath);
            if (FileBrowserHelpers.GetFilesize(zipPath) < MaxUploadFileSize)
                StartCoroutine(UploadFileToServer(zipPath, true));
            else
            {
                DisplayUploadErrorMessage("File size is too large. It must be less than 50MB.");
            }
        }
        else
        {
            StartCoroutine(UploadFileToServer(dataPath, false));
        }
    }

    /// <summary>
    /// Uploads a single file to the server. Not used in case of upload to AmazonS3
    /// Note - As the backend server supports only upload to AmazonS3 this is never called
    /// </summary>
    /// <param name="uploadFilePath">Path of file to be uploaded</param>
    /// <param name="deleteTempFile">Path of temporary Zip file</param>
    /// <returns></returns>
    private IEnumerator UploadFileToServer(string uploadFilePath, bool deleteTempFile)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        string fileName = FileBrowserHelpers.GetFilename(uploadFilePath);
        byte[] fileData = FileBrowserHelpers.ReadBytesFromFile(uploadFilePath);
        formData.Add(new MultipartFormFileSection("Object", fileData, fileName, "application/octet-stream"));

        using (UnityWebRequest uwr = UnityWebRequest.Post("http://192.168.0.103:5555/fileupload", formData))
        {
            uwr.SendWebRequest();

            MessageFields uploadMsgFields = DisplayUploadProgressMessage();
            StartCoroutine(CheckUploadConnection(uwr));
            while (!uwr.isDone)
            {
                string progress = "Progress : " + Mathf.Round(uwr.uploadProgress * 100).ToString() + "%";
                uploadMsgFields.MessageDetails("Upload Progress ...", progress);
                if (uwr.isNetworkError || uwr.isHttpError)
                {
                    yield break;
                }
                yield return null;
            }

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                DestroyUploadProgressMessage(uploadMsgFields);
                if (deleteTempFile && FileBrowserHelpers.FileExists(uploadFilePath))
                {
                    FileBrowserHelpers.DeleteFile(uploadFilePath);
                    Debug.Log("Temp File deleted after Error");
                }
                DisplayUploadErrorMessage(uwr.error);
            }
            else
            {
                DestroyUploadProgressMessage(uploadMsgFields);
                if (deleteTempFile && FileBrowserHelpers.FileExists(uploadFilePath))
                {
                    FileBrowserHelpers.DeleteFile(uploadFilePath);
                    Debug.Log("Temp File deleted");
                }
            }
        }
    }

    /// <summary>
    /// Checks if the UnityWebRequest made is uploading and responsive.
    /// </summary>
    /// <remarks>
    /// If the Web Request is unresponsive for more than 10 secs it is likely that the internet connection is cut-off
    /// </remarks>
    /// <param name="uwr">UnityWebRequest object</param>
    /// <returns></returns>
    private IEnumerator CheckUploadConnection(UnityWebRequest uwr)
    {
        bool IsAborted = false;
        int counter = 0;
        float previousProgress, currentProgress, deltaProgress;
        while (!uwr.isDone && !IsAborted)
        {
            previousProgress = uwr.uploadProgress;
            yield return new WaitForSeconds(1.0f);
            try
            {
                currentProgress = uwr.uploadProgress;
            }
            catch (ArgumentNullException ex)
            {
                Debug.Log("[Web Request Object is Disposed] : " + ex.Message);
                break;
            }
            deltaProgress = currentProgress - previousProgress;
            if (deltaProgress <= 0.0f)
            {
                counter++;
            }
            else
            {
                counter = 0;
            }
            if (counter > 10)
            {
                Debug.Log("10 Secs elapsed without any progress.");
                if (uwr != null)
                {
                    Debug.Log("Aborted!");
                    IsAborted = true;
                    uwr.Abort();
                }

            }
        }
    }

    /// <summary>
    /// Uploads the File/Folder to AmazonS3
    /// Determines if the user selected path is a file or Folder and calls appropriate Coroutines
    /// </summary>
    /// <param name="dataPath">User Selected path for upload</param>
    public void UploadToAmazonS3(string dataPath)
    {
        if (FileBrowserHelpers.DirectoryExists(dataPath))
        {
            StartCoroutine(UploadFolderToAmazonS3(dataPath));
        }
        else
        {
            string key = FileBrowserHelpers.GetFilename(dataPath);
            StartCoroutine(GetPreSignedRequest(key, dataPath, true, (e, m) => { }));            
        }
    }

    /// <summary>
    /// Uploads the files present in the folder selected by the user
    /// </summary>
    /// <param name="folderPath">Path of folder</param>
    /// <returns></returns>
    IEnumerator UploadFolderToAmazonS3(string folderPath)
    {
        bool errorOccured = false;
        string errorMsg = null;
        string rootName = FileBrowserHelpers.GetFilename(folderPath);
        List<string> uploadFileList = GetUploadFileList(folderPath, out long uploadSize);
        if (uploadSize > MaxUploadFileSize)
        {
            DisplayUploadErrorMessage("File size is too large. It must be less than 50MB.");
            yield break;
        }
        if (uploadFileList.Count > 1)
        {
            int numberOfFiles = uploadFileList.Count;
            float index = 0;
            float uploadPercentage;
            string key, progress;
            MessageFields uploadMsgFields = DisplayUploadProgressMessage();
            foreach (string file in uploadFileList)
            {
                uploadPercentage = (index / numberOfFiles) * 100;
                progress = "Progress : " + Mathf.Round(uploadPercentage).ToString() + "%";
                uploadMsgFields.MessageDetails("Upload Progress ...", progress);
                key = file.Substring(file.LastIndexOf(rootName));
                key = key.Replace("\\", "/");
                yield return GetPreSignedRequest(key, file, false, (e,m) => { errorOccured = e; errorMsg = m; });
                if (errorOccured)
                    break;
                index++;                
            }

            DestroyUploadProgressMessage(uploadMsgFields);

            if (errorOccured)
            {
                DisplayUploadErrorMessage(errorMsg);
                yield break;
            }
            if (gltfUrl != null)
            {
                yield return PushUrlToDatabase(gltfUrl);
            }
        }
        else if (uploadFileList.Count == 1)
        {
            string filePath = uploadFileList[0];
            string key = filePath.Substring(filePath.LastIndexOf(rootName));
            key = key.Replace("\\", "/");
            yield return GetPreSignedRequest(key, filePath, true, (e,m) => { });            
        }
        else
            DisplayUploadErrorMessage("Folder is Empty.");
    }

    /// <summary>
    /// Returns the paths of all the files present in the folder
    /// Note - upload size must be less than MaxUploadSize
    /// </summary>
    /// <param name="folderPath">Folder Path</param>
    /// <param name="uploadSize">Passint the size of all file in Folder as out param</param>
    /// <returns>List of all the file paths present in given folder</returns>
    List<string> GetUploadFileList(string folderPath, out long uploadSize)
    {
        List<string> filePaths = new List<string>();
        Stack<string> dirs = new Stack<string>();
        long uploadLength = 0;
        if (FileBrowserHelpers.DirectoryExists(folderPath))
        {
            dirs.Push(folderPath);
        }

        while (dirs.Count > 0)
        {
            string currentDir = dirs.Pop();
            FileSystemEntry[] allEntries = FileBrowserHelpers.GetEntriesInDirectory(currentDir);
            foreach (FileSystemEntry entry in allEntries)
            {
                if (entry.IsDirectory)
                {
                    dirs.Push(entry.Path);
                }
                else
                {
                    filePaths.Add(entry.Path);
                    uploadLength += FileBrowserHelpers.GetFilesize(entry.Path);
                    Debug.Log("File : " + entry.Path);
                }
            }
        }

        uploadSize = uploadLength;
        return filePaths;
    }

    /// <summary>
    /// Gets the PreSigned Request to upload a file from the remote server
    /// </summary>
    /// <param name="keyName">KeyName of the file for which to generate the presigned request</param>
    /// <param name="filePath">file path to be uploaded</param>
    /// <param name="isSingleFile">False if it is a part of Folder upload, true otherwise</param>
    /// <param name="ErrorCallback">Used only during Folder upload, callback if any error occurs</param>
    /// <returns></returns>
    IEnumerator GetPreSignedRequest(string keyName, string filePath, bool isSingleFile, Action<bool, string> ErrorCallback)
    {
        bool errorOccured = false;
        string errorMsg = null;
        using (UnityWebRequest uwr = UnityWebRequest.Get(amazonS3ServerBackend + "/sign_request?file_name=" + keyName))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                if(isSingleFile)
                    DisplayUploadErrorMessage(uwr.error);
                else
                    ErrorCallback(true, uwr.error + "\n" + keyName);
            }                
            else
            {
                Debug.Log("Sign Request success for : " + keyName);
                string resp = uwr.downloadHandler.text;
                string updatedResp = resp.Replace("x-amz-algorithm", "xamzalgorithm");
                updatedResp = updatedResp.Replace("x-amz-credential", "xamzcredential");
                updatedResp = updatedResp.Replace("x-amz-date", "xamzdate");
                updatedResp = updatedResp.Replace("x-amz-signature", "xamzsignature");
                PresignResponse pr = JsonUtility.FromJson<PresignResponse>(updatedResp);
                yield return UploadFileToAmazonS3(pr, filePath, isSingleFile, (error, message) => { errorOccured = error; errorMsg = message; });
                if (errorOccured)
                {
                    ErrorCallback(errorOccured, errorMsg);
                }
            }
        }        
    }

    /// <summary>
    /// Actually uploads the file to AmazonS3 after getting a presigned request for that file
    /// </summary>
    /// <param name="resp">Response of Presigned Request</param>
    /// <param name="filePath">Path of the file to be uploaded</param>
    /// <param name="isSingleFile">False if it is a part of Folder upload, true otherwise</param>
    /// <param name="ErrorCallback">Used only during Folder upload, callback if any error occurs</param>
    /// <returns></returns>
    IEnumerator UploadFileToAmazonS3(PresignResponse resp, string filePath, bool isSingleFile, Action<bool, string> ErrorCallback)
    {
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        byte[] fileData = FileBrowserHelpers.ReadBytesFromFile(filePath);
        string filename = FileBrowserHelpers.GetFilename(filePath);

        formData.Add(new MultipartFormDataSection(nameof(resp.fields.acl), resp.fields.acl));
        formData.Add(new MultipartFormDataSection(nameof(resp.fields.key), resp.fields.key));
        formData.Add(new MultipartFormDataSection("x-amz-algorithm", resp.fields.xamzalgorithm));
        formData.Add(new MultipartFormDataSection("x-amz-credential", resp.fields.xamzcredential));
        formData.Add(new MultipartFormDataSection("x-amz-date", resp.fields.xamzdate));
        formData.Add(new MultipartFormDataSection(nameof(resp.fields.policy), resp.fields.policy));
        formData.Add(new MultipartFormDataSection("x-amz-signature", resp.fields.xamzsignature));
        formData.Add(new MultipartFormFileSection("file", fileData, filename, "application/octet-stream"));
        using (UnityWebRequest uwr = UnityWebRequest.Post(resp.url, formData))
        {
            if (isSingleFile)
            {
                uwr.SendWebRequest();

                MessageFields uploadMsgFields = DisplayUploadProgressMessage();
                StartCoroutine(CheckUploadConnection(uwr));
                while (!uwr.isDone)
                {
                    string progress = "Progress : " + Mathf.Round(uwr.uploadProgress * 100).ToString() + "%";
                    uploadMsgFields.MessageDetails("Upload Progress ...", progress);
                    if (uwr.isNetworkError || uwr.isHttpError)
                    {
                        DestroyUploadProgressMessage(uploadMsgFields);
                        yield break;
                    }
                    yield return null;
                }
                DestroyUploadProgressMessage(uploadMsgFields);
            }
            else
            {
                yield return uwr.SendWebRequest();
            }               

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                if(isSingleFile)
                    DisplayUploadErrorMessage(uwr.error);
                else
                    ErrorCallback(true, uwr.error + "\n" + resp.fields.key);
            }                
            else
            {
                Debug.Log("File Upload Success! : " + filename);
                //Debug.Log(uwr.GetResponseHeader("Location"));
                if (filename.EndsWith(TypeGLTF) || filename.EndsWith(TypeGLB))
                {
                    gltfUrl = uwr.GetResponseHeader("Location");
                    gltfUrl = UnityWebRequest.UnEscapeURL(gltfUrl);
                    if (isSingleFile)
                        yield return PushUrlToDatabase(gltfUrl);
                }                
            }
        }
    }

    /// <summary>
    /// Sends the saved file location in AmazonS3 to remote server to genrate a WebAR link
    /// </summary>
    /// <param name="uploadUrl">file location of object in AmazonS3</param>
    /// <returns></returns>
    IEnumerator PushUrlToDatabase(string uploadUrl)
    {
        gltfUrl = null;
        List<IMultipartFormSection> form = new List<IMultipartFormSection>();
        form.Add(new MultipartFormDataSection("Location", uploadUrl));
        using (UnityWebRequest uwr = UnityWebRequest.Post(amazonS3ServerBackend + "/save_url", form))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                DisplayUploadErrorMessage(uwr.downloadHandler.text);
            }
            else
            {
                DisplayARUrlMessage(uwr.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Displays the error message occured during file upload
    /// </summary>
    /// <param name="errorMessage">Error Message to be displayed</param>
    public void DisplayUploadErrorMessage(string errorMessage)
    {
        GameObject uploadErrorMessage = UIManager.Instance.CreateMessageWindow();
        if (uploadErrorMessage != null)
        {
            MessageFields msgFields = uploadErrorMessage.GetComponent<MessageFields>();
            msgFields.MessageDetails("Upload File Error!", errorMessage, "OK");
            Transform okTrans = uploadErrorMessage.transform.Find("Done");
            if (okTrans != null)
            {
                Button okButton = okTrans.gameObject.GetComponent<Button>();
                okButton.onClick.AddListener(() => { Destroy(uploadErrorMessage); });
            }
        }
    }

    /// <summary>
    /// Display the WebAR link as hyperlink to the user
    /// </summary>
    /// <param name="url">WebAR link to be displayed</param>
    void DisplayARUrlMessage(string url)
    {
        GameObject arUrlMessage = UIManager.Instance.CreateMessageWindow();
        if(arUrlMessage != null)
        {
            MessageFields msgFields = arUrlMessage.GetComponent<MessageFields>();
            msgFields.MessageDetails("WebAR URL", "Copy and Paste the below URL in a Web Browser.\n" + "<link=" + url + "><color=blue><u><i>" + url + "</i></color></u></link>", "OK");
            Transform okTrans = arUrlMessage.transform.Find("Done");
            if(okTrans != null)
            {
                Button okButton = okTrans.gameObject.GetComponent<Button>();
                okButton.onClick.AddListener(() => { Destroy(arUrlMessage); });
            }
        }
    }

    /// <summary>
    /// Used to display the progress of file upload
    /// </summary>
    /// <returns>Message Fields object that display the actual message, paasing it as it needs to be updated in a loop</returns>
    public MessageFields DisplayUploadProgressMessage()
    {
        GameObject uploadProgressMessage = UIManager.Instance.CreateMessageWindow();
        if (uploadProgressMessage != null)
        {
            MessageFields msgFields = uploadProgressMessage.GetComponent<MessageFields>();
            msgFields.MessageDetails("Upload Progress ...", "Progress : ");
            return msgFields;
        }
        return null;
    }

    /// <summary>
    /// Destroys the uploadProgressMessage gameObject
    /// </summary>
    /// <param name="msgFields">Message Fiels of the uploadProgressMessage</param>
    public void DestroyUploadProgressMessage(MessageFields msgFields)
    {
        if (msgFields.gameObject != null)
            Destroy(msgFields.gameObject);
    }
}
