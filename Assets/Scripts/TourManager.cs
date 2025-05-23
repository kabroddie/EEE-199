using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TourManager : MonoBehaviour
{
    public enum TourState
    {
        Inactive,        // Tour mode is OFF
        HeadingToStart,  // User is navigating to the starting point
        WaitingForScan,  // Waiting for QR scan confirmation at the starting point
        ReadyForTour,    // User clicked "Ready for Tour" → Tour starts
        TourActive,       // Actively navigating through POIs
    }

    public TourState currentState = TourState.Inactive;

    [SerializeField]
    private PullUpUI pullupUI;
    [SerializeField]
    private NavigationController navigationController;

    [SerializeField]
    private TargetHandler targetHandler;

    [SerializeField]
    private GameObject tourPromptPanel;

    [SerializeField]
    private TMP_Text promptText;

    [SerializeField]
    private GameObject qrScanPromptTextObject;

    [SerializeField]
    private GameObject readyForTourButton;
    [SerializeField]
    private GameObject map;

    [SerializeField]
    private GameObject binaryChoices;
    [SerializeField]
    private GameObject okayButton;
    [SerializeField]
    private GameObject bottomBar;
    [SerializeField]
    private Button resumeButton;
    private AudioManager audioManager;

    public GameObject TourOptions;
    public GameObject StartTourButton;

    public TargetFacade startingPoint;
    private Vector3 tourStartingPoint = Vector3.zero;
    private List<TargetFacade> selectedTourPOIs = new List<TargetFacade>();

    private float arrivalThreshold = 2.0f;
    private int currentPOIIndex = 0;

    // Add this at the top of your TourManager class
    private string lastTourType = "";

    [SerializeField] private GameObject exitConfirmation;
    [SerializeField] private TextMeshProUGUI exitConfirmationText;
    [SerializeField] private TextMeshProUGUI exitConfirmationTextbf;

    private void Start()
    {
        audioManager = FindObjectOfType<AudioManager>();

        qrScanPromptTextObject.SetActive(false);
        readyForTourButton.SetActive(false);
        exitConfirmation.SetActive(false);
        binaryChoices.SetActive(false);
        okayButton.SetActive(false);

        ClearTourProgress(); // Clear progress on start

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeTourFromButton);

        // LoadTourProgress(); // Try to resume if data exists
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
            StartTourButton.SetActive(false);
            TourOptions.SetActive(true);
        }
        else
        {
            ExitTourMode();
            
        }
    }

    private void StartTourMode(string tourType)
    {
        // if (currentState != TourState.Inactive) return;

        lastTourType = tourType;
        Debug.Log($"Switching to Tour Mode: {lastTourType}...");

        switch (tourType)
        {

            case "1F Tour":
                selectedTourPOIs = targetHandler.GetAllFirstFloorPresetPath();
                startingPoint = targetHandler.GetCurrentTargetByTargetText("MeralcoHall");
                break;

            case "2F Tour":
                selectedTourPOIs = targetHandler.GetAllSecondFloorPresetPath();
                startingPoint = targetHandler.GetCurrentTargetByTargetText("Old2ndStair");
                break;

            case "1F and 2F Tour":
                selectedTourPOIs = targetHandler.GetFirstAndSecondFloorPresetPath();
                startingPoint = targetHandler.GetCurrentTargetByTargetText("MeralcoHall");
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
            return;
        }

        if (currentState != TourState.Inactive)
        {
            StartTour();
        }
        else
        {
            currentState = TourState.HeadingToStart;
            StartCoroutine(audioManager.PlayHeadingToStart());
            navigationController.ActivateNavigation(tourStartingPoint);
            SaveTourProgress(tourType); // Save the tour type before starting
        }

        // StartToffur();
    }

    private void ShowQRScanPrompt()
    {
        bottomBar.SetActive(false);
        map.SetActive(false);
        qrScanPromptTextObject.SetActive(true);
    }

    public void OnQRCodeScanned(string scannedText)
    {
        if (currentState == TourState.WaitingForScan)
        {
            if (startingPoint == null)
            {
                Debug.LogWarning("[TourManager] No starting point set, ignoring scan.");
                return;
            }

            if (scannedText == startingPoint.Name)
            {
                OnQRCodeScannedAtStartingPoint();
            }
        }
    }

    public void OnQRCodeScannedAtStartingPoint()
    {
        if (currentState == TourState.WaitingForScan)
        {
            qrScanPromptTextObject.SetActive(false);
            readyForTourButton.SetActive(true);
            bottomBar.SetActive(true);
        }
    }

    public bool IsTourActive()
    {
        return currentState == TourState.TourActive;
    }

    public void OnReadyForTour()
    {
        StartCoroutine(audioManager.PlayTouring());
        readyForTourButton.SetActive(false);
        map.SetActive(true);
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
            promptText.text = selectedTourPOIs[currentPOIIndex].Name;
        }
    }

    public void OnNextPOI()
    {
        if (currentPOIIndex < selectedTourPOIs.Count)
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
        SaveTourProgress(lastTourType); // Save before exiting
        Debug.Log("Exiting tour mode...");
        currentState = TourState.Inactive;
        navigationController.ResetNavigation();
        targetHandler.HideAllPins();
        tourPromptPanel.SetActive(false);
        qrScanPromptTextObject.SetActive(false);
        readyForTourButton.SetActive(false);
        exitConfirmation.SetActive(false);
        StartTourButton.SetActive(true);
        pullupUI.ExpandPanel();
        TourOptions.SetActive(false);
        map.SetActive(true);
    }

    public TourState GetCurrentState()
    {
        return currentState;
    }

    public void SaveTourProgress(string tourType)
    {
        PlayerPrefs.SetString("TourType", tourType);
        PlayerPrefs.SetInt("TourPOIIndex", currentPOIIndex);
        PlayerPrefs.SetInt("TourState", (int)currentState);
        PlayerPrefs.Save();
    }

    public bool LoadTourProgress()
    {
        if (!PlayerPrefs.HasKey("TourType")) return false;

        string tourType = PlayerPrefs.GetString("TourType");
        int poiIndex = PlayerPrefs.GetInt("TourPOIIndex");
        TourState state = (TourState)PlayerPrefs.GetInt("TourState");

        currentPOIIndex = poiIndex;
        currentState = state;
        StartTourMode(tourType);

        // Resume navigation if tour is active
        if (currentState == TourState.TourActive && currentPOIIndex < selectedTourPOIs.Count)
            navigationController.ActivateNavigation(selectedTourPOIs[currentPOIIndex].transform.position);
        else if (currentState == TourState.HeadingToStart)
            navigationController.ActivateNavigation(tourStartingPoint);

        return true;
    }

    public bool HasTourProgress()
    {
        return PlayerPrefs.HasKey("TourType") && PlayerPrefs.HasKey("TourPOIIndex") && PlayerPrefs.HasKey("TourState");
    }

    // Add this to your TourManager class
    public void ResumeTourFromButton()
    {
        if (!HasTourProgress())
        {
            ExitConfirmationPanel("non-resume");
            return;
        }

        pullupUI.ClosePanel();
        StartTourButton.SetActive(false);
        TourOptions.SetActive(true);
        bool loaded = LoadTourProgress();
    }

    public void ClearTourProgress()
    {
        PlayerPrefs.DeleteKey("TourType");
        PlayerPrefs.DeleteKey("TourPOIIndex");
        PlayerPrefs.DeleteKey("TourState");
        currentState = TourState.Inactive;
        pullupUI.ExpandPanel();
        StartTourButton.SetActive(true);
        TourOptions.SetActive(false);

        PlayerPrefs.Save();
        navigationController.ResetNavigation();
    }

    public void ExitConfirmationPanel(string action)
    {
        binaryChoices.SetActive(false);
        okayButton.SetActive(false);  
        if (HasTourProgress())
        {
            if (action == "exit")
            {
                pullupUI.ClosePanel();
                exitConfirmation.SetActive(true);
                exitConfirmationText.text = "Are you sure you want to exit the tour?";
                exitConfirmationTextbf.text = "You're exiting tour mode...";
                map.SetActive(false);
            }
            else if (action == "reset")
            {
                pullupUI.ClosePanel();
                exitConfirmation.SetActive(true);
                exitConfirmationText.text = "Are you sure you want to clear your tour progress?";
                exitConfirmationTextbf.text = "You're resetting your tour progress...";
                map.SetActive(false);
            }
            binaryChoices.SetActive(true);

        }
        else
        {
            if (action == "exit")
            {
                
                exitConfirmation.SetActive(true);
                exitConfirmationText.text = "You are not in tour mode.";
                exitConfirmationTextbf.text = "Uh-oh...";
                map.SetActive(false);
            }
            else if (action == "reset")
            {
                exitConfirmation.SetActive(true);
                exitConfirmationText.text = "There is no tour progress to clear.";
                exitConfirmationTextbf.text = "Uh-oh...";
                map.SetActive(false);
            }
            else if (action == "non-resume")
            {
                exitConfirmation.SetActive(true);
                exitConfirmationText.text = "You have not started a tour yet.";
                exitConfirmationTextbf.text = "Uh-oh...";
                map.SetActive(false);
            }
            okayButton.SetActive(true);
        }
        
        
        
    }

    public void ConfirmExit()
    {
        if (exitConfirmationTextbf.text == "You're exiting tour mode...")
        {
            ExitTourMode();
            exitConfirmation.SetActive(false);
        }
        else if (exitConfirmationTextbf.text == "You're resetting your tour progress...")
        {
            ClearTourProgress();
            exitConfirmation.SetActive(false);
        }
        else if (exitConfirmationTextbf.text == "Uh-oh...")
        {
            exitConfirmation.SetActive(false);
        }
        Debug.Log("Tour progress cleared.");
        map.SetActive(true);
 
    }

    public void CancelExit()
    {
        exitConfirmation.SetActive(false);
        map.SetActive(true);
    }   
}
