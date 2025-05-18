using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates an indicator that points to the navigation path when it's off-screen or behind the user
/// </summary>
public class OffscreenNavLine : MonoBehaviour
{
    [Header("References")]
    public Camera arCamera;
    public LineRenderer lineRenderer;

    [Header("UI")]
    public RectTransform indicatorUI;
    public Canvas canvas;

    [Header("Settings")]
    public float edgePadding = 100f;

    private void Update()
    {
        if (lineRenderer == null || arCamera == null || indicatorUI == null || canvas == null)
            return;

        int pointCount = lineRenderer.positionCount;
        if (pointCount == 0)
            return;

        Vector3 targetWorldPos = lineRenderer.GetPosition(pointCount - 1);
        Vector3 viewportPos = arCamera.WorldToViewportPoint(targetWorldPos);

        // Flip behind-camera targets to front
        if (viewportPos.z < 0)
        {
            viewportPos *= -1;
        }

        bool isVisible = viewportPos.z > 0 &&
                         viewportPos.x >= 0 && viewportPos.x <= 1 &&
                         viewportPos.y >= 0 && viewportPos.y <= 1;

        indicatorUI.gameObject.SetActive(!isVisible);

        if (!isVisible)
        {
            ShowIndicatorOnEdge(targetWorldPos);
        }
    }

    private void ShowIndicatorOnEdge(Vector3 worldPos)
    {
        Vector3 screenPos = arCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0) screenPos *= -1; // Behind camera, flip

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 fromCenter = (Vector2)screenPos - screenCenter;

        float maxRadius = Mathf.Min(screenCenter.x, screenCenter.y) - edgePadding;
        Vector2 clampedDirection = Vector2.ClampMagnitude(fromCenter, maxRadius);
        Vector2 finalScreenPos = screenCenter + clampedDirection;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            finalScreenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 canvasPos
        );

        indicatorUI.localPosition = canvasPos;

        float angle = Mathf.Atan2(clampedDirection.y, clampedDirection.x) * Mathf.Rad2Deg;
        indicatorUI.localRotation = Quaternion.Euler(0f, 0f, angle - 90f); // Arrow points up
    }
}