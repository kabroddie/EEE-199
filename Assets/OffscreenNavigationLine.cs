using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates an indicator that points to the navigation path when it's off-screen or behind the user
/// </summary>
public class OffScreenIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject indicatorPrefab;    // The arrow UI prefab
    [SerializeField] private Transform navigationTarget;    // Reference to the navigation line or target point
    [SerializeField] private Camera arCamera;               // The AR camera (usually the main camera)
    [SerializeField] private Canvas uiCanvas;               // Canvas where the indicator will be displayed
    
    [Header("Settings")]
    [SerializeField] private float indicatorOffset = 50f;   // Distance from screen edge
    [SerializeField] private bool showOnlyWhenBehind = true; // If true, only shows when path is behind the user
    [SerializeField] private float minimumDistance = 1f;    // Minimum distance to show the indicator
    
    private RectTransform canvasRect;
    private RectTransform indicatorRect;
    private GameObject indicator;
    
    private void Start()
    {
        if (arCamera == null) arCamera = Camera.main;
        if (uiCanvas == null) uiCanvas = FindObjectOfType<Canvas>();
        
        canvasRect = uiCanvas.GetComponent<RectTransform>();
        
        // Create indicator
        if (indicatorPrefab != null)
        {
            indicator = Instantiate(indicatorPrefab, uiCanvas.transform);
            indicatorRect = indicator.GetComponent<RectTransform>();
            indicator.SetActive(false);
        }
        else
        {
            Debug.LogError("Indicator prefab is not assigned!");
        }
    }
    
    private void Update()
    {
        if (navigationTarget == null || indicator == null) return;
        
        // Position check
        Vector3 targetPositionViewport = arCamera.WorldToViewportPoint(navigationTarget.position);
        float distanceToTarget = Vector3.Distance(arCamera.transform.position, navigationTarget.position);
        
        // Check if target is behind the camera
        bool isBehind = targetPositionViewport.z < 0;
        
        // Determine if indicator should be shown
        bool shouldShow = (isBehind || !IsInScreen(targetPositionViewport)) && 
                          (!showOnlyWhenBehind || isBehind) && 
                          distanceToTarget > minimumDistance;
        
        // Show/hide indicator
        indicator.SetActive(shouldShow);
        
        if (shouldShow)
        {
            // If behind, flip the coordinates
            if (isBehind)
            {
                targetPositionViewport.x = 1.0f - targetPositionViewport.x;
                targetPositionViewport.y = 1.0f - targetPositionViewport.y;
            }
            
            // Calculate screen position
            Vector2 screenPosition = new Vector2(
                targetPositionViewport.x * canvasRect.sizeDelta.x,
                targetPositionViewport.y * canvasRect.sizeDelta.y);
            
            // Clamp position to screen bounds with offset
            screenPosition = ClampToScreen(screenPosition);
            
            // Set indicator position
            indicatorRect.anchoredPosition = screenPosition;
            
            // Calculate rotation to point towards target
            Vector2 screenCenter = new Vector2(canvasRect.sizeDelta.x * 0.5f, canvasRect.sizeDelta.y * 0.5f);
            float angle = Mathf.Atan2(screenPosition.y - screenCenter.y, screenPosition.x - screenCenter.x) * Mathf.Rad2Deg;
            indicatorRect.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    private bool IsInScreen(Vector3 viewportPos)
    {
        return viewportPos.x >= 0 && viewportPos.x <= 1 && 
               viewportPos.y >= 0 && viewportPos.y <= 1;
    }
    
    private Vector2 ClampToScreen(Vector2 position)
    {
        // Get screen dimensions
        float halfWidth = canvasRect.sizeDelta.x * 0.5f;
        float halfHeight = canvasRect.sizeDelta.y * 0.5f;
        
        // Calculate center-based coordinates
        float centerX = position.x - halfWidth;
        float centerY = position.y - halfHeight;
        
        // Calculate angle from center to position
        float angle = Mathf.Atan2(centerY, centerX);
        
        // Calculate max distance in that angle
        float maxX = Mathf.Cos(angle) * (halfWidth - indicatorOffset);
        float maxY = Mathf.Sin(angle) * (halfHeight - indicatorOffset);
        
        // If outside screen bounds, clamp to edge
        if (Mathf.Abs(centerX) > Mathf.Abs(maxX) || Mathf.Abs(centerY) > Mathf.Abs(maxY))
        {
            centerX = maxX;
            centerY = maxY;
        }
        
        // Convert back to corner-based coordinates
        return new Vector2(centerX + halfWidth, centerY + halfHeight);
    }
    
    // Call this to update the navigation target
    public void SetNavigationTarget(Transform target)
    {
        navigationTarget = target;
    }
}