using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening; // Import DOTween

public class PullUpUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public RectTransform panel;  // Assign UI Panel in Inspector
    public SettingsMenu settingsMenu; // Reference to SettingsMenu script
    public float openY;  // Y position when fully opened
    public float closedY; // Y position when closed
    public float tweenTime = 0.35f; // Animation speed (adjust for mobile smoothness)
    public float swipeThreshold = 50f; // Minimum swipe speed to trigger open/close

    private Vector2 dragStartPosition;
    private float dragStartTime;
    private bool isExpanded = false;
    private bool isAnimating = false;


    private void Start()
    {
        // Set initial position (closed)
        panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, closedY);
        
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isAnimating) return; // Prevent dragging while animating
        dragStartPosition = panel.anchoredPosition;
        dragStartTime = Time.time; // Record drag start time for swipe speed calculation
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isAnimating) return; // Prevent dragging when animating

        // Allow dragging within limits
        float newY = Mathf.Clamp(dragStartPosition.y + eventData.delta.y, closedY, openY);
        panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, newY);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isAnimating) return;

        float dragDuration = Time.time - dragStartTime; // Calculate swipe speed
        float dragDistance = panel.anchoredPosition.y - dragStartPosition.y;
        float swipeSpeed = Mathf.Abs(dragDistance / dragDuration);

        // If swipe was fast enough, override position-based decision
        if (swipeSpeed > swipeThreshold)
        {
            if (dragDistance > 0) ExpandPanel(); // Swiped up
            else ClosePanel(); // Swiped down
        }
        else
        {
            // Normal behavior: Expand or Close based on position
            if (panel.anchoredPosition.y > (closedY + openY) / 2)
                ExpandPanel();
            else
                ClosePanel();
        }
    }

    public void ExpandPanel()
    {
        if (!isExpanded && !isAnimating)
        {
            isAnimating = true;
            panel.DOAnchorPosY(openY, tweenTime)
                 .SetEase(Ease.OutCubic)
                 .OnComplete(() => isAnimating = false);
            isExpanded = true;
        }

        settingsMenu.CollapseMenu();
    }

    public void ClosePanel()
    {
        if (isExpanded && !isAnimating)
        {
            isAnimating = true;
            panel.DOAnchorPosY(closedY, tweenTime)
                 .SetEase(Ease.InCubic)
                 .OnComplete(() => isAnimating = false);
            isExpanded = false;
        }
    }

    // Optional: Button toggle function
    public void TogglePanel()
    {
        if (isExpanded) ClosePanel();
        else ExpandPanel();
    }
}
