using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MinimapController : MonoBehaviour, IDragHandler, IScrollHandler
{

    public Camera minimapCamera;
    public ButtonGroupHandler buttonGroupHandler;

    [System.Serializable]
    public struct FloorBounds
    {
        public float cameraX;
        public Vector2 minBounds;
        public Vector2 maxBounds;
    }

    public FloorBounds[] floors;

    public float dragSpeed = 0.5f;
    private Vector2 minBounds;
    private Vector2 maxBounds;

    public float zoomSpeed = 0.5f;
    public float minZoom = 50f;
    public float maxZoom = 100f;

    public GameObject buttonBack;
    public GameObject buttonNext;


    private int counter = 0;
    private bool isMovingFloor = false;
    // Start is called before the first frame update
    void Start()
    {
        MoveToFloorByIndex();
        if(counter == 0){
            buttonBack.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandlePinchZoom();

        if (counter == floors.Length - 1){
            buttonBack.transform.position = buttonNext.transform.position;
            buttonNext.SetActive(false); 
        } else {
            buttonNext.SetActive(true);
        }


        if(counter == 0){
            buttonBack.SetActive(false);
        } else {
            buttonBack.SetActive(true);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isMovingFloor) return;
        if (minimapCamera == null) return;

        Vector2 delta = eventData.delta;
        Vector3 movement = new Vector3(-delta.x, 0f, -delta.y) * dragSpeed * Time.deltaTime;
        minimapCamera.transform.position += movement;

        ClampCameraPosition();
    }
    public void OnScroll(PointerEventData eventData)
    {
        float scroll = eventData.scrollDelta.y;
        minimapCamera.orthographicSize = Mathf.Clamp(
            minimapCamera.orthographicSize - scroll * zoomSpeed * 50f * Time.deltaTime,
            minZoom,
            maxZoom
        );
    }

    public void Buttons(bool next){
        if(next){
            Debug.Log("Next");
            if(counter < floors.Length - 1 & counter >= 0){
                counter++;
                buttonGroupHandler.SetFloorNumber(counter);
                MoveToFloorByIndex();
            }
        } else {
            if (counter > 0){
                counter--;
                buttonGroupHandler.SetFloorNumber(counter);
                MoveToFloorByIndex();
            }
        }
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

    public void MoveToFloorByIndex()
    {
        if (counter >= 0 && counter < floors.Length)
        {
            MoveToFloorXSmooth(floors[counter].cameraX, floors[counter].minBounds, floors[counter].maxBounds);
        }
    }

    private void MoveToFloorXSmooth(float xPosition, Vector2 newMinBounds, Vector2 newMaxBounds, float duration = 0.5f)
    {
        // Set new bounds immediately
        minBounds = newMinBounds;
        maxBounds = newMaxBounds;

        // Start smooth movement
        Vector3 targetPos = new Vector3(xPosition, minimapCamera.transform.position.y, minimapCamera.transform.position.z);
        StartCoroutine(SmoothMove(minimapCamera.transform, targetPos, duration));
    }

    private IEnumerator SmoothMove(Transform target, Vector3 end, float duration)
    {
        isMovingFloor = true;
        Vector3 start = target.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            target.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.position = end;
        isMovingFloor = false;
    }


}
