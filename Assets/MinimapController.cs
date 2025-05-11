using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MinimapController : MonoBehaviour, IDragHandler, IScrollHandler
{

    public Camera minimapCamera;

    public float dragSpeed = 0.5f;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    public float zoomSpeed = 0.5f;
    public float minZoom = 50f;
    public float maxZoom = 100f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        HandlePinchZoom();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (minimapCamera == null) return;

        Vector2 delta = eventData.delta;
        Vector3 movement = new Vector3(-delta.x, 0f, -delta.y) * dragSpeed * Time.deltaTime;
        minimapCamera.transform.position += movement;

        ClampCameraPosition();
    }
    public void OnScroll(PointerEventData eventData)
    {
        // Optional scroll wheel support (for PC debug)
        float scroll = eventData.scrollDelta.y;
        minimapCamera.orthographicSize = Mathf.Clamp(
            minimapCamera.orthographicSize - scroll * zoomSpeed * 50f * Time.deltaTime,
            minZoom,
            maxZoom
        );
    }

    void HandlePinchZoom()
    {
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            Vector2 prevT0 = t0.position - t0.deltaPosition;
            Vector2 prevT1 = t1.position - t1.deltaPosition;

            float prevDist = Vector2.Distance(prevT0, prevT1);
            float currentDist = Vector2.Distance(t0.position, t1.position);

            float delta = currentDist - prevDist;

            minimapCamera.orthographicSize = Mathf.Clamp(
                minimapCamera.orthographicSize - delta * zoomSpeed,
                minZoom,
                maxZoom
            );
        }
    }

    void ClampCameraPosition()
    {
        Vector3 pos = minimapCamera.transform.position;
        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.z = Mathf.Clamp(pos.z, minBounds.y, maxBounds.y); 
        minimapCamera.transform.position = pos;

    }

}
