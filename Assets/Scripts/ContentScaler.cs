using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Attached to the imported object that supports scaling the object with pinch
/// </summary>
public class ContentScaler : MonoBehaviour
{
    private Vector3 originalScale;
    private float _minScale = 1.0f;
    private float _maxScale = 2000.0f;
    private float scaleValue = 1.0f;
    private float speed = 0.3f;

    private void Start()
    {
        originalScale = transform.localScale;
    }

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

            float additiveScale = scaleValue * 100.0f;
            additiveScale -= deltaMagDiff * speed;
            scaleValue = Mathf.Clamp(additiveScale, _minScale, _maxScale) / 100.0f;
            Debug.Log("Scale : " + scaleValue);

            transform.localScale = originalScale * scaleValue;
        }
    }
}
