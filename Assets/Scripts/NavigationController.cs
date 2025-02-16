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
    private Transform userTransform; // ✅ Added reference to user transform

    private bool navigationActive = false;
    private bool hasTarget = false;
    private float arrivalThreshold = 1.0f; // Distance threshold for arrival

    // Start is called before the first frame update
    private void Start()
    {
        path = new NavMeshPath();
        // disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    // Update is called once per frame
    private void Update()
    {
        if (navigationActive && hasTarget)
        { 
            NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, path);
            line.positionCount = path.corners.Length;
            line.SetPositions(path.corners);

            UpdateLineVisibility();

            if (Vector3.Distance(transform.position, targetPosition) < arrivalThreshold)
            {
                HandleArrival();
            }
        }
    }

    private void UpdateLineVisibility()
    {
        for (int i = 0; i < line.positionCount; i++)
        {
            Vector3 point = line.GetPosition(i);
            if (IsBehindWall(point))
            {
                line.enabled = false;
                targetHandler.TogglePinVisibility(targetPosition, false); // ✅ Hide pin if line is hidden
                return;
            }
        }
        line.enabled = true;
        targetHandler.TogglePinVisibility(targetPosition, true); // ✅ Ensure pin is visible if line is visible

    }

    private bool IsBehindWall(Vector3 worldPosition)
    {
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPosition);
        Ray ray = Camera.main.ScreenPointToRay(screenPoint);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
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

    // Custom method to check if a point is inside the AR plane's boundary
    private bool IsPointInsideBoundary(Vector3 worldPos, NativeArray<Vector2> boundary)
    {
        int count = boundary.Length;
        if (count < 3) return false; // Not a valid boundary

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

    public void ActivateNavigation(Vector3 newTarget)
    {
        if (targetPosition == newTarget && navigationActive)
        {
            ToggleNavigation(); 
            return;
        }

        targetHandler.TogglePinVisibility(targetPosition, false); // ✅ Hide previous pin if it exists

        targetPosition = newTarget;
        hasTarget = true;
        navigationActive = true;
        line.enabled = true;

        targetHandler.TogglePinVisibility(targetPosition, true);
        UpdateToggleButtonText();
    }

    public void ToggleNavigation()
    {
        if (!hasTarget) return; // ✅ Prevent toggling if no target is selected

        navigationActive = !navigationActive;

        if (!navigationActive)
        {
            line.enabled = false;
            targetHandler.HideAllPins();
        }
        else
        {
            line.enabled = true;
            targetHandler.TogglePinVisibility(targetPosition, true); // ✅ Ensure pin reappears when toggling back
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

    private void HandleArrival()
    {
        line.enabled = false;
        hasTarget = false;
        navigationActive = false;
        targetHandler.HideAllPins();
        UpdateToggleButtonText();
    }
}
