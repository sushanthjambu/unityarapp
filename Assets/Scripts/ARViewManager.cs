using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using SimpleFileBrowser;

/// <summary>
/// Controls the Viewer Scene/AR Scene
/// </summary>
public class ARViewManager : Singleton<ARViewManager>
{
    /// <summary>
    /// ARFoundation's Raycast Manager used to raycast to ground plane
    /// </summary>
    [SerializeField]
    ARRaycastManager arRaycastManager;

    /// <summary>
    /// ARFoundation's Plane Manager used to detect the ground Plane
    /// </summary>
    [SerializeField]
    ARPlaneManager arPlaneManager;

    /// <summary>
    /// ARFoundation's Anchor Manager used to place an AR Anchor
    /// </summary>
    [SerializeField]
    ARAnchorManager arAnchorManager;

    /// <summary>
    /// Holds the imported object
    /// </summary>
    GameObject _placedObject;

    /// <summary>
    /// Determines if the object is placed on the plane
    /// </summary>
    public bool IsObjectPlaced { get; private set; }

    /// <summary>
    /// Stores the Raycast hits on the plane
    /// </summary>
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    /// <summary>
    /// Loaded object is given to Viewer Scene as _placedObject
    /// </summary>
    /// <param name="importedObject">Object that is imported</param>
    public void AssignObject(GameObject importedObject)
    {
        if (importedObject != null)
        {
            _placedObject = importedObject;
            IsObjectPlaced = false;
        }
           
    }

    /// <summary>
    /// Record the user touch
    /// </summary>
    /// <param name="touchPosition">Returns user touch position as</param>
    /// <returns>True if user has touched the screen else False</returns>
    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if(Input.touchCount > 0)
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }

        touchPosition = default;
        return false;
    }

    private void Update()
    {
        if (!TryGetTouchPosition(out Vector2 touchPosition) || IsObjectPlaced || _placedObject == null)
            return;

        if (arRaycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            var hitPose = hits[0].pose;
            GameObject instantiatedObject = Instantiate(_placedObject, hitPose.position, hitPose.rotation);
            instantiatedObject.SetActive(true);
            instantiatedObject.AddComponent<ContentScaler>();
            IsObjectPlaced = true;
            ARPlane hitPlane = arPlaneManager.GetPlane(hits[0].trackableId);
            if (hitPlane != null)
            {
                Debug.Log("Plane is found");
                ARAnchor anchor = arAnchorManager.AttachAnchor(hitPlane, hitPose);
                Debug.Log("Anchor is attached to plane : " + anchor.name);
                anchor.transform.SetParent(instantiatedObject.transform);
            }
            StopPlaneDetection();
        }

    }

    /// <summary>
    /// If user clicks the Export Icon in the top right corner
    /// </summary>
    public void OnExport()
    {
        StartCoroutine(GameManager.Instance.DisplaySaveCoroutine());
    }

    void StopPlaneDetection()
    {
        arPlaneManager.enabled = false;
        foreach(var plane in arPlaneManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }        
    }
    
}
