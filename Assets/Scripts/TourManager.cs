using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TourManager : MonoBehaviour
{
    private enum TourState
    {
        Inactive,        // Tour mode is OFF
        HeadingToStart,  // User is navigating to the starting point
        WaitingForScan,  // Waiting for QR scan confirmation at the starting point
        ReadyForTour,    // User clicked "Ready for Tour" → Tour starts
        TourActive       // Actively navigating through POIs
    }

    private TourState currentState = TourState.Inactive;

    [SerializeField] private NavigationController navigationController;
    [SerializeField] private TargetHandler targetHandler;
    [SerializeField] private GameObject tourPromptPanel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private GameObject qrScanPromptTextObject; // ✅ Floating text for scanning QR code
    [SerializeField] private GameObject readyForTourButton; // ✅ Button for user to confirm readiness

    private Vector3 tourStartingPoint;
    private List<Vector3> tourPOIs = new List<Vector3>();
    private float arrivalThreshold = 1.0f;
    private int currentPOIIndex = 0;

    private void Start()
    {
        SetStartingPoint(); // ✅ Find and set the "Entry" QR code as the starting point
        tourPOIs = targetHandler.GetNonQRTargetPositions(); // ✅ Get POI locations
        qrScanPromptTextObject.SetActive(false); // Hide QR scan message initially
        readyForTourButton.SetActive(false); // Hide "Ready for Tour" button initially
    }

    private void SetStartingPoint()
    {
        TargetFacade startingPoint = targetHandler.GetCurrentTargetByTargetText("Entry");
        if (startingPoint != null)
        {
            Debug.Log("Setting starting point...");
            tourStartingPoint = startingPoint.transform.position;
        }
    }

    private void Update()
    {
        if (currentState == TourState.HeadingToStart)
        {
            if (Vector3.Distance(navigationController.transform.position, tourStartingPoint) < arrivalThreshold)
            {
                currentState = TourState.WaitingForScan;
                ShowQRScanPrompt();
            }
        }
    }

    public void ToggleTourMode()
    {
        if (currentState == TourState.Inactive)
        {
            StartTourMode();
        }
        else
        {
            ExitTourMode();
        }
    }

    private void StartTourMode()
    {
        if (currentState != TourState.Inactive) return;

        Debug.Log("Switching to Tour Mode...");
        currentState = TourState.HeadingToStart;
        navigationController.ActivateNavigation(tourStartingPoint);
    }

    private void ShowQRScanPrompt()
    {
        qrScanPromptTextObject.SetActive(true); // ✅ Floating text: "Please scan the QR code."
    }

    public void OnQRCodeScanned(string scannedText)
    {
        if (currentState == TourState.WaitingForScan && scannedText == "Entrance") // ✅ Ensure it matches the defined starting point
        {
            OnQRCodeScannedAtStartingPoint();
        }
    }

    public void OnQRCodeScannedAtStartingPoint()
    {
        if (currentState == TourState.WaitingForScan)
        {
            qrScanPromptTextObject.SetActive(false); // ✅ Hide QR scan floating text
            readyForTourButton.SetActive(true); // ✅ Enable "Ready for Tour" button
        }
    }

    public bool IsTourActive()
    {
        return currentState == TourState.TourActive;
    }


    public void OnReadyForTour()
    {
        readyForTourButton.SetActive(false);
        StartTour();
    }

    private void StartTour()
    {
        if (tourPOIs.Count == 0)
        {
            return;
        }

        currentState = TourState.TourActive;
        currentPOIIndex = 0;
        NavigateToNextPOI();
    }

    private void NavigateToNextPOI()
    {
        if (currentPOIIndex < tourPOIs.Count)
        {
            Debug.Log($"Navigating to POI {currentPOIIndex + 1} of {tourPOIs.Count}...");
            navigationController.ActivateNavigation(tourPOIs[currentPOIIndex]);
        }
        else
        {
            EndTour();
        }
    }

    public void OnArrivalAtPOI()
    {
        if (currentState == TourState.TourActive)
        {
            tourPromptPanel.SetActive(true);
            promptText.text = $"You have reached {tourPOIs[currentPOIIndex]}.\nPress 'Next' to continue or 'Exit' to leave the tour.";
        }
    }

    public void OnNextPOI()
    {
        tourPromptPanel.SetActive(false);
        currentPOIIndex++;
        NavigateToNextPOI();
    }

    public void EndTour()
    {
        Debug.Log("Tour completed!");
        currentState = TourState.Inactive;
        navigationController.ToggleNavigation();
        tourPromptPanel.SetActive(false);
    }

    public void ExitTourMode()
    {
        Debug.Log("Exiting tour mode...");
        currentState = TourState.Inactive;
        navigationController.ToggleNavigation();
        targetHandler.HideAllPins();
        tourPromptPanel.SetActive(false);
        qrScanPromptTextObject.SetActive(false);
        readyForTourButton.SetActive(false);
    }
}
