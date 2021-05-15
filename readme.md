## AR Viewer Android App (Unity)
* An android app made with Unity that can import 3D models (.obj, .gltf, .glb) present on the device and display them as an AR object after tapping on the detected ground plane.
* Generate Web links that can display the selected 3D model (only .gltf and .glb files are supported for this feature) as an AR object in Web Browser. These links can be shared with anyone so that they can view the AR content on their browser.
	> **Note** : If the browser or the device doesn't support AR then only the 3D Model is dispayed.
* Export the imported models to a location on your android device as .gltf model. So you can first import a .obj object view in AR and then export it to a location on the device as .gltf model.
	> **Note** : The exported model may not contain all the textures/features of the imported model. The app is limited by the libraries it uses.