using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


public class ARViewManager : Singleton<ARViewManager>
{
    [SerializeField]
    ARRaycastManager arRaycastManager;

    GameObject _placedObject;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    public void AssignObject(GameObject importedObject)
    {
        if (importedObject != null)
        {
            _placedObject = importedObject;
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
        if (!TryGetTouchPosition(out Vector2 touchPosition) || _placedObject == null)
            return;

        if (arRaycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            var hitPose = hits[0].pose;
            Instantiate(_placedObject, hitPose.position, hitPose.rotation).SetActive(true);
        }

    }
}
