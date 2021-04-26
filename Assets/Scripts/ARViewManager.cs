using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using SimpleFileBrowser;


public class ARViewManager : Singleton<ARViewManager>
{
    [SerializeField]
    ARRaycastManager arRaycastManager;

    [SerializeField]
    ARPlaneManager arPlaneManager;

    [SerializeField]
    ARAnchorManager arAnchorManager;

    GameObject _placedObject;

    public bool IsObjectPlaced { get; private set; }

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    public void AssignObject(GameObject importedObject)
    {
        if (importedObject != null)
        {
            _placedObject = importedObject;
            IsObjectPlaced = false;
        }
           
    }

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
        }

    }

    public void OnExport()
    {
        StartCoroutine(GameManager.Instance.DisplaySaveCoroutine());
    }
    
}
