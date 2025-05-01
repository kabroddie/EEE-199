using UnityEngine;
using TMPro;    // only if you’re using TextMeshPro for the prompt

public class AltitudeDetector : MonoBehaviour
{
    [SerializeField]
    public Transform trackedTransform;

    [SerializeField]
    public float thresholdY = 1.5f;

    [SerializeField]
    private GameObject altitudeChangedPanel;

    [SerializeField]
    private TextMeshProUGUI altitudeChangedText; // Only if using TextMeshPro for the prompt

    [SerializeField]
    private GameObject map;

    [SerializeField]
    private QrCodeRecenter qrCodeScanner;

    void Start()
    {
        // Hide at start
        if (altitudeChangedPanel != null)
            altitudeChangedPanel.SetActive(false);
    }

    void Update()
    {
        if (altitudeChangedPanel == null || trackedTransform == null) return;

        // Check if we've crossed the Y threshold
        if (trackedTransform.position.y >= thresholdY || trackedTransform.position.y <= -thresholdY)
        {
            ShowPrompt();
        }
    }

    private void ShowPrompt()
    {
        map.SetActive(false); // Hide the map when the prompt is shown
        altitudeChangedPanel.SetActive(true);
        altitudeChangedText.text = $"It seems you changed floors.\n\nKindly please scan the QR code near the staircase.\n\nThank you!"; // Update the prompt text
    }

    private void HidePrompt()
    {
        altitudeChangedPanel.SetActive(false);
        qrCodeScanner.ToggleScanning(); // Stop scanning when the prompt is hidden
    }
    public void OnQRCodeScanned()
    {
        // Hide the prompt when the QR code is scanned
        HidePrompt();
    }
}
