## AR Viewer Android App (Unity)
* An android app made with Unity that can import 3D models (.obj, .gltf, .glb) present on the device and display them as an AR object after tapping on the detected ground plane.
* Generate Web links that can display the selected 3D model (only .gltf and .glb files are supported for this feature) as an AR object in Web Browser. These links can be shared with anyone so that they can view the AR content on their browser.
	> **Note** : If the browser or the device doesn't support AR then only the 3D Model is dispayed.
<img src="https://github.com/sushanthjambu/readme-images/blob/main/unityarapp/WebAR%20Link.png" alt="WebAR URL" width="20%" height="50%">
* Export the imported models to a location on your android device as .gltf model. So you can first import a .obj object view in AR and then export it to a location on the device as .gltf model.
	> **Note** : The exported model may not contain all the textures/features of the imported model. The app is limited by the libraries/packages it uses.
<img src="https://github.com/sushanthjambu/readme-images/blob/main/unityarapp/Export%20Button.png" alt="Export" width="20%" height="50%">

## Features
There are two main features that this app implements
1. Imports the local 3D files of type .obj, .gltf and .glb and displays in AR View.
	- After placing the object on a plane you can also export the 3D object to .gltf format. However the exported object may not have all the properties of the original object.
2. WebAR - Generates a Web link which can be shared. So that anyone can view the 3D object in web browser for which you have generated the web link.

## Installation
- You can find the .apk file in the releases section that can be installed on your Android phone. But the WebAR feature will not work this way, read the WebAR section below to know more.
- You may clone this repo or download the zip and open it via Unity Editor. You can also run/play the project in Unity Editor.

## Unity Version and Packages used
- Unity - 2020.1
- AR Foundation - 3.1.10
- AR Core - 3.1.8
#### Some of the  external packages used are
- **Unity Simple File Browser** - UI File Browser as a unity package that works Androis, iOS etc. It has some really useful helper functions that have been used extensively in this project.
	https://github.com/yasirkula/UnitySimpleFileBrowser
- **Runtime Obj importer** - used for importing .obj models at runtime. It is a really good importer.
	https://assetstore.unity.com/packages/tools/modeling/runtime-obj-importer-49547
- **Unity GLTF** - used for importing and exporting .gltf models at runtime. Using the unity package version from the releases.
	https://github.com/KhronosGroup/UnityGLTF

## AR Viewer
- You may click the "Browse" button on home screen.Also you can open the Options Panel by clicking the button on top left and then click on AR Viewer button.

	<img src="https://github.com/sushanthjambu/readme-images/blob/main/unityarapp/Browse.png" alt="Browse button" width="20%" height="50%">
- After that select the appropriate 3D File to view in AR.
- Sometimes the importer may throw an error or the textures are not imported properly. The app just uses external packages for importing hence nothing can be done in these cases.    

## WebAR
- Implementing WebAR is not direct. You must also implement the Server side funtionality for this to work.
#### Working of feature
1. The app uploads the user selected file to a remote server. In my case an Amazon S3 storage bucket.
2. After successfull upload the server stores the file's S3 storage location in database. Later, a WebAR link is generated by the server.
3. When this link is opened in the browser, the server renders a html file that uses Model Viewer to display the object in AR.
4. So the server gets the uploaded 3D file from Amazon S3 while rendering html in browser.
#### Steps to upload the file/folder successfully
1. User can select either a .glb file or .gltf file. If the .gltf has supporting files like .bin or texture images in a folder, then user must upload the folder containing all the supporting files.
2. However the cumulative size cannot exceed 50MB limit. You may change this in [FileUploader.cs](Assets/Scripts/FileUploader.cs#L31) to suit your needs.
3. To select a folder, open the folder you want to upload and without selecting any file in that folder just click on "Upload" button. Make sure the folder name is present in the Adreess bar before you upload.
#### Steps to implement the Server
- I have an example Server implemented with Flask-python. Below is the repo
	- https://github.com/sushanthjambu/jarviewer-flask
- **After you implement the Server give the public url of server in [FileUploader.cs](Assets/Scripts/FileUploader.cs#L19) script.**

## Credits
- Unity Simple File Browser - https://github.com/yasirkula/UnitySimpleFileBrowser
- Runtime Obj Importer - https://assetstore.unity.com/packages/tools/modeling/runtime-obj-importer-49547
- Unity GLTF - https://github.com/KhronosGroup/UnityGLTF