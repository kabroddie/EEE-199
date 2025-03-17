using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TourManager : MonoBehaviour
{
    public enum TourState
    {
        Inactive,        // Tour mode is OFF
        HeadingToStart,  // User is navigating to the starting point
        WaitingForScan,  // Waiting for QR scan confirmation at the starting point
        ReadyForTour,    // User clicked "Ready for Tour" → Tour starts
        TourActive       // Actively navigating through POIs
    }

    public TourState currentState = TourState.Inactive;

    [SerializeField] private NavigationController navigationController;
    [SerializeField] private TargetHandler targetHandler;
    [SerializeField] private GameObject tourPromptPanel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private GameObject qrScanPromptTextObject;
    [SerializeField] private GameObject readyForTourButton;

    private TargetFacade startingPoint;
    private Vector3 tourStartingPoint = Vector3.zero;
    private List<TargetFacade> selectedTourPOIs = new List<TargetFacade>();

    private float arrivalThreshold = 1.0f;
    private int currentPOIIndex = 0;

    private void Start()
    {
        qrScanPromptTextObject.SetActive(false);
        readyForTourButton.SetActive(false);
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

    public void ToggleTourMode(string tourType)
    {
        if (currentState == TourState.Inactive)
        {
            StartTourMode(tourType);
        }
        else
        {
            ExitTourMode();
        }
    }

    private void StartTourMode(string tourType)
    {
        if (currentState != TourState.Inactive) return;

        Debug.Log($"Switching to Tour Mode: {tourType}...");

        switch (tourType)
        {
            case "Old Bldg 1F Tour":
                selectedTourPOIs = targetHandler.GetOldFirstFloorPresetPath();
                startingPoint = targetHandler.GetCurrentTargetByTargetText("Entry");
                break;

            case "New Bldg 1F Tour":
                selectedTourPOIs = targetHandler.GetNewFirstFloorPresetPath();
                startingPoint = targetHandler.GetCurrentTargetByTargetText("MeralcoHall");
                break;

            case "1F Tour":
                selectedTourPOIs = targetHandler.GetAllFirstFloorPresetPath();
                startingPoint = targetHandler.GetCurrentTargetByTargetText("Entry");
                break;

            default:
                Debug.LogWarning($"[TourManager] Unknown tour type: {tourType}");
                return;
        }

        if (selectedTourPOIs.Count == 0)
        {
            Debug.LogWarning($"[TourManager] No POIs found for tour type: {tourType}");
            return;
        }

        if (startingPoint != null)
        {
            tourStartingPoint = startingPoint.transform.position;
        }
        else
        {
            Debug.LogWarning($"[TourManager] Starting point not found for tour type: {tourType}");
            tourStartingPoint = Vector3.zero;
            return;
        }

        Debug.Log("Starting Tour...");
        currentState = TourState.HeadingToStart;
        navigationController.ActivateNavigation(tourStartingPoint);
    }

    private void ShowQRScanPrompt()
    {
        qrScanPromptTextObject.SetActive(true);
    }

    public void OnQRCodeScanned(string scannedText)
    {
        if (currentState == TourState.WaitingForScan && scannedText == startingPoint.Name)
        {
            OnQRCodeScannedAtStartingPoint();
        }
    }

    public void OnQRCodeScannedAtStartingPoint()
    {
        if (currentState == TourState.WaitingForScan)
        {
            qrScanPromptTextObject.SetActive(false);
            readyForTourButton.SetActive(true);
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
        if (selectedTourPOIs.Count == 0)
        {
            Debug.LogWarning("[TourManager] No POIs available for the tour.");
            return;
        }

        currentState = TourState.TourActive;
        currentPOIIndex = 0;
        NavigateToNextPOI();
    }

    private void NavigateToNextPOI()
    {
        if (currentPOIIndex >= selectedTourPOIs.Count)
        {
            EndTour();
            return;
        }

        navigationController.ActivateNavigation(selectedTourPOIs[currentPOIIndex].transform.position);
    }

    public void OnArrivalAtPOI()
    {
        if (currentState == TourState.TourActive)
        {
            tourPromptPanel.SetActive(true);
            promptText.text = $"You have reached {selectedTourPOIs[currentPOIIndex].Name}.\nPress 'Next' to continue or 'Exit' to leave the tour.";
        }
    }

    public void OnNextPOI()
    {
        if (currentPOIIndex < selectedTourPOIs.Count - 1)
        {
            tourPromptPanel.SetActive(false);
            currentPOIIndex++;
            NavigateToNextPOI();
        }
        else
        {
            EndTour();
        }
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

    public TourState GetCurrentState()
    {
        return currentState;
    }
}
