using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates an indicator that points to the navigation path when it's off-screen or behind the user
/// </summary>
public class OffscreenNavLine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject indicatorPrefab;    // The arrow UI prefab
    [SerializeField] private Transform navigationTarget;    // Reference to the navigation line or target point
    [SerializeField] private Camera arCamera;               // The AR camera (usually the main camera)
    [SerializeField] private Canvas uiCanvas;               // Canvas where the indicator will be displayed
    
    [Header("Settings")]
    [SerializeField] private float indicatorOffset = 50f;   // Distance from screen edge
    [SerializeField] private bool showOnlyWhenBehind = false; // If true, only shows when path is behind the user
    [SerializeField] private float minimumDistance = 1f;    // Minimum distance to show the indicator
    [SerializeField] private bool debugMode = true;         // Enable debug logs
    [SerializeField] private Color indicatorColor = Color.white; // Color of the indicator
    [SerializeField] private float indicatorScale = 1f;     // Scale of the indicator
    
    private RectTransform canvasRect;
    private RectTransform indicatorRect;
    private GameObject indicator;
    private Image indicatorImage;
    
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
            
            // Ensure the indicator has an Image component
            indicatorImage = indicator.GetComponent<Image>();
            if (indicatorImage == null)
            {
                indicatorImage = indicator.AddComponent<Image>();
                Debug.LogWarning("Adding Image component to indicator as none was found");
            }
            
            // Set initial properties
            indicatorImage.color = indicatorColor;
            indicatorRect.localScale = Vector3.one * indicatorScale;
            
            // Ensure it's at the top of the hierarchy for proper rendering
            indicator.transform.SetAsLastSibling();
            
            // Make sure it starts inactive
            indicator.SetActive(false);
            
            if (debugMode)
            {
                Debug.Log("Indicator created: " + indicator.name);
            }
        }
        else
        {
            Debug.LogError("Indicator prefab is not assigned!");
        }
        
        if (debugMode)
        {
            Debug.Log($"OffscreenNavLine initialized with showOnlyWhenBehind={showOnlyWhenBehind}");
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
        bool isOffScreen = !IsInScreen(targetPositionViewport);
        
        // Determine if indicator should be shown
        bool shouldShow;
        
        if (showOnlyWhenBehind)
        {
            // Only show when behind the user
            shouldShow = isBehind && distanceToTarget > minimumDistance;
        }
        else
        {
            // Show when either behind or off-screen
            shouldShow = (isBehind || isOffScreen) && distanceToTarget > minimumDistance;
        }
        
        // Force update the indicator visibility
        indicator.SetActive(shouldShow);
        
        if (shouldShow)
        {
            if (debugMode)
            {
                Debug.Log($"Showing indicator. IsBehind: {isBehind}, IsOffScreen: {isOffScreen}, Distance: {distanceToTarget}");
            }
            
            // If behind, flip the coordinates and place at bottom center of screen for behind indicator
            if (isBehind)
            {
                // For behind indicators, position at the bottom center of the screen
                Vector2 screenPosition = new Vector2(canvasRect.sizeDelta.x * 0.5f, indicatorOffset);
                indicatorRect.anchoredPosition = screenPosition;
                
                // Point downward
                indicatorRect.rotation = Quaternion.Euler(0, 0, 270);
                
                // Make it stand out
                if (indicatorImage != null)
                {
                    indicatorImage.color = Color.red; // Make behind indicators red
                }
                
                return;
            }
            
            // For regular off-screen indicators (not behind)
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
            
            // Reset color for regular indicators
            if (indicatorImage != null)
            {
                indicatorImage.color = indicatorColor;
            }
        }
    }
    
    private bool IsInScreen(Vector3 viewportPos)
    {
        return viewportPos.x >= 0 && viewportPos.x <= 1 && 
               viewportPos.y >= 0 && viewportPos.y <= 1 &&
               viewportPos.z > 0; // Must be in front of camera
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
    public void UpdateNavigationTarget(Transform target)
    {
        navigationTarget = target;
    }
    
    // Use this to toggle debug mode
    public void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
    }
    
    // Call this to toggle showing indicator only when behind
    public void SetShowOnlyWhenBehind(bool onlyWhenBehind)
    {
        showOnlyWhenBehind = onlyWhenBehind;
        if (debugMode)
        {
            Debug.Log($"ShowOnlyWhenBehind set to {showOnlyWhenBehind}");
        }
    }
    
#if UNITY_EDITOR
    // For debugging in editor
    private void OnDrawGizmos()
    {
        if (arCamera != null && navigationTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(arCamera.transform.position, navigationTarget.position);
        }
    }
#endif
}