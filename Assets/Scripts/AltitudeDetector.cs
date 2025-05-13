using UnityEngine;
using TMPro;    // only if you’re using TextMeshPro for the prompt

public class AltitudeDetector : MonoBehaviour
{
    [SerializeField]
    public Transform trackedTransform;

    [SerializeField]
    public float thresholdY;

    [SerializeField]
    private GameObject altitudeChangedPanel;

    [SerializeField]
    private TextMeshProUGUI altitudeChangedText; // Only if using TextMeshPro for the prompt

    [SerializeField]
    private GameObject map;

    [SerializeField]
    private QrCodeRecenter qrCodeScanner;
    
    [SerializeField]
    public GameObject confirmationPanel; // Reference to the confirmation panel

    [SerializeField]
    private FloorTransitionManager floorTransitionManager; // Reference to the FloorTransitionManager

    public bool altitudeHasChanged = false;

    private float lastY;

    void Start()
    {
        // Hide at start
        if (altitudeChangedPanel != null)
            altitudeChangedPanel.SetActive(false);
    }

    void Update()
    {
        float currentY = trackedTransform.position.y;

        // Only fire the moment we cross up or down through the threshold:
        if (!altitudeHasChanged)
        {
            bool crossedUp   = lastY <  thresholdY && currentY >=  thresholdY;
            bool crossedDown = lastY > -thresholdY && currentY <= -thresholdY;

            if ((crossedUp || crossedDown) && 
                (floorTransitionManager.GetCurrentState() == FloorTransitionManager.FloorState.Idle ||
                 floorTransitionManager.GetCurrentState() == FloorTransitionManager.FloorState.NavigatingNewFloor))
             {
                ShowPrompt();
                altitudeHasChanged = true;
            }
           
        }

        lastY = currentY;
    }

    private void ShowPrompt()
    {
        map.SetActive(false); // Hide the map when the prompt is shown
        altitudeChangedPanel.SetActive(true);
        altitudeChangedText.text = $"It seems you changed floors.\n\nKindly please scan the QR code near the staircase.\n\nThank you!"; // Update the prompt text
    }

    public void ConfirmationPrompt()
    {
        // Hide the prompt when the user confirms the action
        altitudeChangedPanel.SetActive(false);
        confirmationPanel.SetActive(true); // Reset the flag
    }

    public void OnQRCodeScanned()
    {
        // Hide the prompt when the QR code is scanned
        confirmationPanel.SetActive(false);
        altitudeChangedPanel.SetActive(false);
        altitudeHasChanged = false; // Reset the flag
        qrCodeScanner.ToggleScanning(); // Resume scanning
    }
}
