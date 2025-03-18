using UnityEngine;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.AI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;

public class NavigationController : MonoBehaviour
{
    public Vector3 targetPosition { get; set; } = Vector3.zero;

    [SerializeField]
    private LineRenderer line;
    
    private NavMeshPath path;

    [SerializeField]
    private TextMeshProUGUI toggleButtonText;

    [SerializeField]
    private ARPlaneManager planeManager;

    [SerializeField]
    private TargetHandler targetHandler;

    [SerializeField]
    private Transform userTransform; // Reference to user transform

    [SerializeField]
    private ARRaycastManager raycastManager; // Attach AR Raycast Manager

    // [SerializeField]
    // private Slider navigationYOffset; // Slider for dynamic height adjustment

    private TourManager tourManager;

    private FloorTransitionManager floorTransitionManager;

    private bool navigationActive = false;
    private bool hasTarget = false;
    private float arrivalThreshold = 1.0f; // Distance threshold for arrival

    // Dynamic height variables
    private float lineHeightOffset = 0.2f;  

    // ✅ NEW: Pin prefab for the end of the line
    [SerializeField] private GameObject pinPrefab;
    private GameObject dynamicPin; // Holds the instantiated pin

    private Dictionary<int, Queue<float>> heightHistory = new Dictionary<int, Queue<float>>();
    private int historySize = 5; // ✅ Store the last 5 heights per point

    private void Start()
    {
        path = new NavMeshPath();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        tourManager = FindObjectOfType<TourManager>();
        floorTransitionManager = FindObjectOfType<FloorTransitionManager>();

        if (floorTransitionManager == null)
        {
            Debug.LogError("[NavigationController] ❌ FloorTransitionManager not found in the scene!");
        }

        // ✅ Instantiate the pin once, hide it at first
        if (pinPrefab != null)
        {
            dynamicPin = Instantiate(pinPrefab);
            dynamicPin.SetActive(false);
        }
    }

    private void Update()
    {
        if (navigationActive && hasTarget)
        { 

            // Calculate the path
            NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, path);

            // ✅ Store a modified version of path.corners
            Vector3[] adjustedPath = new Vector3[path.corners.Length];
            for (int i = 0; i < path.corners.Length; i++)
            {
                adjustedPath[i] = path.corners[i]; // Copy original path
            }

            // ✅ Apply modified path to the line before dynamic height adjustment
            line.positionCount = adjustedPath.Length;
            line.SetPositions(adjustedPath);

             // ✅ Now dynamically adjust height based on AR planes only
            // AdjustLineHeightUsingRaycast(adjustedPath);

            // // Update the line
            // line.positionCount = path.corners.Length;
            

            // Check line visibility
            UpdateLineVisibility();

            // Adjust line height dynamically
            // AdjustLineHeight();
            // Vector3[] calculatePathandOffset = AddLineOffset();
            // line.SetPositions(calculatePathandOffset);
            

            // ✅ Update the pin position to the end of the line
            UpdatePinPosition();

            CheckArrival(); // ✅ Now checks if the user arrived at a POI

            // // Arrival check
            // if (Vector3.Distance(transform.position, targetPosition) < arrivalThreshold)
            // {
            //     HandleArrival();
            // }
        }
    }

    private void CheckArrival()
    {
        if (!navigationActive || !hasTarget)
            return;

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget < arrivalThreshold)
        {
            TargetFacade arrivedTarget = targetHandler.GetCurrentTargetByPosition(targetPosition);
            if (arrivedTarget != null)
            {
                HandleArrival();
                if (floorTransitionManager != null && floorTransitionManager.IsTransitionPOI(arrivedTarget.Name))
                {
                    // ✅ User has arrived at a transition POI
                    floorTransitionManager.OnArrivedAtPOI(arrivedTarget.Name);
                }
            }
        }
    }


/// <summary>
/// Adjusts the height of the line renderer dynamically using AR Depth API.
/// </summary>
    // private void AdjustLineHeightUsingRaycast(Vector3[] adjustedPath)
    // {
    //     if (adjustedPath.Length == 0 || raycastManager == null) return; // ✅ Safety check

    //     List<ARRaycastHit> hits = new List<ARRaycastHit>();

    //     for (int i = 0; i < adjustedPath.Length; i++)
    //     {
    //         Vector3 point = adjustedPath[i];
    //         float adjustedY = point.y;

    //         // ✅ Convert world position to screen position
    //         Vector2 screenPos = Camera.main.WorldToScreenPoint(point);

    //         // ✅ Perform AR Raycast to detect real-world floor height
    //         if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinBounds))
    //         {
    //             adjustedY = hits[0].pose.position.y + lineHeightOffset;
    //         }

    //         // ✅ Apply smooth transition to prevent sudden jumps
    //         adjustedPath[i] = new Vector3(point.x, Mathf.Lerp(point.y, adjustedY, Time.deltaTime * heightAdjustmentSpeed), point.z);
    //     }

    //     // ✅ Apply updated path to LineRenderer
    //     line.SetPositions(adjustedPath);
    // }
    private void AdjustLineHeightUsingRaycast(Vector3[] adjustedPath)
    {
        if (adjustedPath.Length == 0 || raycastManager == null) return;

        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        for (int i = 0; i < adjustedPath.Length; i++)
        {
            Vector3 point = adjustedPath[i];
            float detectedY = point.y;

            Vector2 screenPos = Camera.main.WorldToScreenPoint(point);
            if (raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinBounds))
            {
                detectedY = hits[0].pose.position.y + lineHeightOffset;
            }

            // ✅ Maintain height history for smoothing
            if (!heightHistory.ContainsKey(i))
            {
                heightHistory[i] = new Queue<float>();
            }

            Queue<float> history = heightHistory[i];
            if (history.Count >= historySize)
            {
                history.Dequeue(); // Remove oldest height value
            }
            history.Enqueue(detectedY);

            // ✅ Use the average of stored height values for stability
            float smoothedY = history.Average();

            adjustedPath[i] = new Vector3(point.x, smoothedY, point.z);
        }

        line.SetPositions(adjustedPath);
    }


//     /// <summary>
//     /// Checks if any point is behind a wall, toggles line/pin visibility
//     /// </summary>
    private void UpdateLineVisibility()
    {
        for (int i = 0; i < line.positionCount; i++)
        {
            Vector3 point = line.GetPosition(i);
            if (IsBehindWall(point))
            {
                line.enabled = false;
                targetHandler.TogglePinVisibility(targetPosition, false);
                
                // ✅ Hide pin if line is hidden
                if (dynamicPin != null) dynamicPin.SetActive(false);
                return;
            }
        }

        line.enabled = true;
        targetHandler.TogglePinVisibility(targetPosition, true);
        
        // ✅ If navigation is active, ensure pin is visible
        if (dynamicPin != null && navigationActive) dynamicPin.SetActive(true);
    }

    /// <summary>
    /// Dynamically position the pin at the last corner of the line
    /// </summary>
    private void UpdatePinPosition()
    {
        if (!navigationActive || !line.enabled) 
            return;

        if (line.positionCount > 0)
        {
            // Get the last corner of the line
            Vector3 lastCorner = line.GetPosition(line.positionCount - 1);

            // Offset it slightly above the line
            dynamicPin.transform.position = lastCorner + Vector3.up * 0.5f; 
            
            // Ensure pin is visible if the line is active
            dynamicPin.SetActive(true);
        }
    }

    /// <summary>
    /// Raycast logic to detect if the point is behind a wall
    /// </summary>
    private bool IsBehindWall(Vector3 worldPosition)
    {
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPosition);
        Ray ray = Camera.main.ScreenPointToRay(screenPoint);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                return true;
            }
        }

        foreach (var plane in planeManager.trackables)
        {
            if (IsPointInsideBoundary(worldPosition, plane.boundary))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a point is inside the AR plane boundary
    /// </summary>
    private bool IsPointInsideBoundary(Vector3 worldPos, NativeArray<Vector2> boundary)
    {
        int count = boundary.Length;
        if (count < 3) return false;

        Vector2 point2D = new Vector2(worldPos.x, worldPos.z);
        bool inside = false;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 pi = boundary[i];
            Vector2 pj = boundary[j];

            if (((pi.y > point2D.y) != (pj.y > point2D.y)) &&
                (point2D.x < (pj.x - pi.x) * (point2D.y - pi.y) / (pj.y - pi.y) + pi.x))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// Activates navigation to a new target
    /// </summary>
    public void ActivateNavigation(Vector3 newTarget)
    {
        TargetFacade target = targetHandler.GetCurrentTargetByPosition(newTarget);
        Debug.Log($"[NavigationController] 🎯 Activating navigation to {target.Name}");


        if (target == null)
        {
            Debug.LogWarning("[NavigationController] ❌ Target POI not found!");
            return;
        }

        // ✅ If the target is on a different floor, let FloorTransitionManager handle it
        if (floorTransitionManager != null && target.Floor != floorTransitionManager.currentFloor)
        {
            Debug.Log($"[NavigationController] 🏢 POI '{target.Name}' is on Floor {target.Floor}, transitioning...");
            floorTransitionManager.OnPOISelected(target.Name);
            return; // ✅ Do not continue normal navigation, FloorTransitionManager takes over
        }

        

        if (targetPosition == newTarget && navigationActive)
        {
            ToggleNavigation(); 
            return;
        }

        targetHandler.TogglePinVisibility(targetPosition, false);
        targetPosition = newTarget;
        hasTarget = true;
        navigationActive = true;
        line.enabled = true;

        targetHandler.TogglePinVisibility(targetPosition, true);
        UpdateToggleButtonText();

        // ✅ Show pin if we have one
        if (dynamicPin != null) dynamicPin.SetActive(true);
    }

    /// <summary>
    /// Toggles line & pin visibility
    /// </summary>
    public void ToggleNavigation()
    {
        if (!hasTarget) return;

        navigationActive = !navigationActive;

        if (!navigationActive)
        {
            line.enabled = false;
            targetHandler.HideAllPins();

            // ✅ Hide pin
            if (dynamicPin != null) dynamicPin.SetActive(false);
        }
        else
        {
            line.enabled = true;
            targetHandler.TogglePinVisibility(targetPosition, true);
            
            // ✅ Show pin
            if (dynamicPin != null) dynamicPin.SetActive(true);
        }

        UpdateToggleButtonText();
    }

    private void UpdateToggleButtonText() 
    {
        if (toggleButtonText != null)
        {
            toggleButtonText.text = navigationActive ? "Line: On" : "Line: Off";
        }
    }

    /// <summary>
    /// Called when user arrives at the destination
    /// </summary>
    public void HandleArrival()
    {
        line.enabled = false;
        hasTarget = false;
        navigationActive = false;
        targetHandler.HideAllPins();
        UpdateToggleButtonText();

        // ✅ Hide pin upon arrival
        if (dynamicPin != null) dynamicPin.SetActive(false);

        if (tourManager != null && tourManager.IsTourActive())
        {
            tourManager.OnArrivalAtPOI();
        }
    }
}
