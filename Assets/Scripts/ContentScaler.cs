using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ContentScaler : MonoBehaviour
{
    private float _minScale = 0.1f;
    private float _maxScale = 30.0f;

    ARSessionOrigin arSessionOrigin;
    Transform sessionOriginTransform;

    GameObject spawnedObject;
    ARAnchor anchor;
    
    // Start is called before the first frame update
    void Start()
    {
        arSessionOrigin = GetComponent<ARSessionOrigin>();
        sessionOriginTransform = arSessionOrigin.transform;
        ARViewManager.OnAnchorAttached += AssignObjectAnchor;
    }

    // Update is called once per frame
    void Update()
    {
        if (!ARViewManager.Instance.IsObjectPlaced)
            return;

        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            float deltaMagDiff = prevTouchDeltaMag - touchDeltaMag;
            Debug.Log("Delta Mag : " + deltaMagDiff);

            float scaleValue = sessionOriginTransform.localScale.x + (deltaMagDiff / 100.0f);
            scaleValue = Mathf.Clamp(scaleValue, _minScale, _maxScale);
            Debug.Log("Scale : " + scaleValue);

            sessionOriginTransform.localScale = Vector3.one * scaleValue;

            //AdjustObjectPosition();

        }
    }

    void AssignObjectAnchor(GameObject placedObject, ARAnchor attachedAnchor)
    {
        spawnedObject = placedObject;
        anchor = attachedAnchor;
    }

    void AdjustObjectPosition()
    {
        if(spawnedObject != null && anchor != null)
        {
            arSessionOrigin.MakeContentAppearAt(spawnedObject.transform, anchor.transform.position);
        }
    }

    private void OnDestroy()
    {
        ARViewManager.OnAnchorAttached -= AssignObjectAnchor;
        spawnedObject = null;
        anchor = null;
    }
}
