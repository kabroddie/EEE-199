using UnityEngine;
using Unity.Collections;
using UnityEngine.UI;
using UnityEngine.AI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class NavigationController : MonoBehaviour
{
    [SerializeField]
    public Vector3 targetPosition { get; set; } = Vector3.zero;

    [SerializeField]
    private LineRenderer line;

    public NavMeshPath path;

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


    private TourManager tourManager;

    private FloorTransitionManager floorTransitionManager;

    private StatusController statusController;
    private AudioManager audioManager;

    public bool navigationActive = false;
    public bool hasTarget = false;
    private float arrivalThreshold = 2.0f; // Distance threshold for arrival

    [SerializeField] private GameObject pinPrefab;
    private GameObject dynamicPin; // Holds the instantiated pin

    [SerializeField] private TextMeshProUGUI arrivedText; // Text to show when the user arrives at a POI
    [SerializeField] private Image aRBar; // Text to show AR info overlay available
    [SerializeField] private TextMeshProUGUI aRBarText; // Text to show AR info overlay available
    private void Start()
    {
        path = new NavMeshPath();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        tourManager = FindObjectOfType<TourManager>();
        floorTransitionManager = FindObjectOfType<FloorTransitionManager>();
        statusController = FindObjectOfType<StatusController>();
        audioManager = FindObjectOfType<AudioManager>();

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

            line.positionCount = path.corners.Length;
            line.SetPositions(path.corners);

            // ✅ Update the pin position to the end of the line
            UpdatePinPosition();

            CheckArrival(); // ✅ Now checks if the user arrived at a POI
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
    }

    public void HandleArrival()
    {
        line.enabled = false;
        hasTarget = false;
        navigationActive = false;
        targetHandler.HideAllPins();

        ArrivalPopUps();

        ArrivalSounds();

        if (dynamicPin != null) dynamicPin.SetActive(false);

        if (tourManager != null && tourManager.IsTourActive())
        {
            tourManager.OnArrivalAtPOI();
        }

    }

    private IEnumerator FadeOutText(TextMeshProUGUI text, float duration)
    {
        Color originalColor = text.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        text.gameObject.SetActive(false);
    }

    private IEnumerator FadeOutBar(Image image, TextMeshProUGUI text, float delayThenFade)
    {
        // Stay fully visible for the delay
        yield return new WaitForSeconds(delayThenFade);

        Color originalColor = image.color;
        Color originalTextColor = text.color;
        float fadeDuration = 1.5f; // You can adjust the fade duration here
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            image.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            text.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        image.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        text.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);
        image.gameObject.SetActive(false);
    }

    /// <summary>
    /// Stops and fully resets navigation (line, pins, flags, UI text).
    /// Call this from your Cancel/Reset button.
    /// </summary>
    public void ResetNavigation()
    {
        Debug.Log("[NavigationController] 🔄 Resetting navigation...");
        // Stop movement updates
        navigationActive = false;
        hasTarget = false;

        // Clear stored target
        targetPosition = Vector3.zero;

        // Disable and clear the line
        if (line != null)
        {
            line.enabled = false;
            line.positionCount = 0;
        }

        // Hide all pins
        if (dynamicPin != null)
            dynamicPin.SetActive(false);

        targetHandler?.HideAllPins();

        if (arrivedText != null)
            arrivedText.gameObject.SetActive(false);

        // if (tourManager != null && tourManager.GetCurrentState() != TourManager.TourState.Inactive)
        // {
        //     tourManager.ExitTourMode();
        // }

        if (floorTransitionManager != null)
        {
            floorTransitionManager.ResetFloor();
        }

        if (statusController != null)
        {
            statusController.HideStatusBar();
        }
    }

    public bool NavigationStatus()
    {
        return navigationActive;
    }

    void ArrivalPopUps()
    {
        TargetFacade arrivedTarget = targetHandler.GetCurrentTargetByPosition(targetPosition);
        if (arrivedTarget != null && targetHandler.ScannablePlaques().Any(x => x.Name == arrivedTarget.Name))
        {
            if (aRBar != null)
            {
                aRBar.gameObject.SetActive(true);
                aRBar.color = new Color(aRBar.color.r, aRBar.color.g, aRBar.color.b, 1f);
                aRBarText.gameObject.SetActive(true);
                aRBarText.color = new Color(aRBarText.color.r, aRBarText.color.g, aRBarText.color.b, 1f);
                StartCoroutine(FadeOutBar(aRBar, aRBarText, 4f));
            }
        }

        arrivedText.gameObject.SetActive(true);
        arrivedText.color = new Color(arrivedText.color.r, arrivedText.color.g, arrivedText.color.b, 1f);
        StartCoroutine(FadeOutText(arrivedText, 4f)); // Fade out text after 2 seconds
    }

    void ArrivalSounds()
    {
        if (floorTransitionManager.GetCurrentState() == FloorTransitionManager.FloorState.NavigatingToTransition &&
                floorTransitionManager.targetFloor == 0)
        {
            StartCoroutine(audioManager.PlayTransitionDown());
        }
        else if (floorTransitionManager.GetCurrentState() == FloorTransitionManager.FloorState.NavigatingToTransition &&
            floorTransitionManager.targetFloor == 1)
        {
            StartCoroutine(audioManager.PlayTransitionUp());
        }
        else
        {
            StartCoroutine(audioManager.PlayArrival());
        }
    }
    
        


}
