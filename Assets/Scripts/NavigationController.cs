using UnityEngine;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.AI;
using TMPro;
using UnityEngine.XR.ARFoundation;

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

    private TourManager tourManager;

    private bool navigationActive = false;
    private bool hasTarget = false;
    private float arrivalThreshold = 1.0f; // Distance threshold for arrival

    // Dynamic height variables
    private float lineHeightOffset = 0.2f; 
    private float heightAdjustmentSpeed = 5f; 

    // ✅ NEW: Pin prefab for the end of the line
    [SerializeField] private GameObject pinPrefab;
    private GameObject dynamicPin; // Holds the instantiated pin

    private void Start()
    {
        path = new NavMeshPath();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        tourManager = FindObjectOfType<TourManager>();

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

            // Update the line
            line.positionCount = path.corners.Length;
            line.SetPositions(path.corners);

            // Check line visibility
            UpdateLineVisibility();

            // Adjust line height dynamically
            AdjustLineHeight();

            // ✅ Update the pin position to the end of the line
            UpdatePinPosition();

            // Arrival check
            if (Vector3.Distance(transform.position, targetPosition) < arrivalThreshold)
            {
                HandleArrival();
            }
        }
    }

    /// <summary>
    /// Dynamically adjusts the line's height based on floor elevation
    /// </summary>
    private void AdjustLineHeight()
    {
        for (int i = 0; i < line.positionCount; i++)
        {
            Vector3 point = line.GetPosition(i);

            // First, check for AR plane height if available
            foreach (var plane in planeManager.trackables)
            {
                if (IsPointInsideBoundary(point, plane.boundary))
                {
                    float targetY = plane.transform.position.y + lineHeightOffset;
                    point.y = Mathf.Lerp(point.y, targetY, Time.deltaTime * heightAdjustmentSpeed);
                    break;
                }
            }

            // If no AR plane, use Raycast to find floor height
            if (Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f))
            {
                float targetY = hit.point.y + lineHeightOffset;
                point.y = Mathf.Lerp(point.y, targetY, Time.deltaTime * heightAdjustmentSpeed);
            }

            line.SetPosition(i, point);
        }
    }

    /// <summary>
    /// Checks if any point is behind a wall, toggles line/pin visibility
    /// </summary>
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
    private void HandleArrival()
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
