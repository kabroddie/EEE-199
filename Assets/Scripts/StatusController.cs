using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatusController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloorTransitionManager floorTransitionManager;
    [SerializeField] private TourManager tourManager;
    [SerializeField] private QrCodeRecenter qrCodeRecenter;
    // [SerializeField] private AppTour appTour; // Reference to the AppTour script

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI floorText; // Or use Image if you have icons
    [SerializeField] private Image statusBarImage; // Use Image instead of CanvasGroup
    [SerializeField] private Image floorCircleImage; // Use Image instead of CanvasGroup

    [Header("Blink Settings")]
    [SerializeField] private float blinkSpeed = 2f;

    private bool shouldBlink = false;
    public bool onboardingFinished = false;

    // Start is called before the first frame update
    void Start()
    {
        // Hide UI at start
        if (statusText != null) statusText.gameObject.SetActive(false);
        if (floorText != null) floorText.gameObject.SetActive(false);
        if (statusBarImage != null) statusBarImage.gameObject.SetActive(false);
        if (floorCircleImage != null) floorCircleImage.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!onboardingFinished)
            return;

        // Always show floor text (and its circle image) after onboarding
        if (floorText != null && !floorText.gameObject.activeSelf) floorText.gameObject.SetActive(true);
        if (floorCircleImage != null && !floorCircleImage.gameObject.activeSelf) floorCircleImage.gameObject.SetActive(true);

        // Update floor display
        if (floorTransitionManager != null && floorText != null)
        {
            floorText.text = $"{floorTransitionManager.GetCurrentFloor() + 1}F";
        }

        // Determine status
        string status = "";
        shouldBlink = false;

        if (tourManager != null && tourManager.GetCurrentState() == TourManager.TourState.TourActive)
        {
            status = "Touring...";
            shouldBlink = true;
        }
        else if (qrCodeRecenter != null && qrCodeRecenter.scanningEnabled)
        {
            status = "Recentering...";
            shouldBlink = false;
        }
        else if (tourManager != null && tourManager.GetCurrentState() == TourManager.TourState.HeadingToStart)
        {
            status = "Heading to Starting Point...";
            shouldBlink = true;
        }
        else if (floorTransitionManager != null &&
                 (floorTransitionManager.GetCurrentState() == FloorTransitionManager.FloorState.FloorTransitionPrompt ||
                  floorTransitionManager.GetCurrentState() == FloorTransitionManager.FloorState.NavigatingNewFloor))
        {
            status = "Transitioning...";
            shouldBlink = true;
        }
        else if (floorTransitionManager != null &&
                 (floorTransitionManager.GetCurrentState() == FloorTransitionManager.FloorState.NavigatingSameFloor ||
                  floorTransitionManager.GetCurrentState() == FloorTransitionManager.FloorState.NavigatingToTransition))
        {
            status = "Navigating...";
            shouldBlink = true;
        }

        // Only show status bar and status text if status is active
        bool showStatus = !string.IsNullOrEmpty(status);

        if (statusBarImage != null)
            statusBarImage.gameObject.SetActive(showStatus);

        if (statusText != null)
        {
            statusText.text = status;
            statusText.gameObject.SetActive(showStatus);
        }

        // Blinking effect for status bar (Image)
        if (statusBarImage != null && showStatus)
        {
            Color color = statusBarImage.color;
            if (shouldBlink)
            {
                color.a = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            }
            else
            {
                color.a = 1f;
            }
            statusBarImage.color = color;
        }
    }

    // Call this from your onboarding script (e.g., AppTour) when onboarding is finished
    public void ShowStatusUI()
    {
        onboardingFinished = true;
    }

    public void HideStatusBar()
    {
        if (statusBarImage != null)
            statusBarImage.gameObject.SetActive(false);
        if (statusText != null)
            statusText.gameObject.SetActive(false);
    }
}
