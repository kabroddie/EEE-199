using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using ZXing;
using System.Collections;


public class QrCodeRecenter : MonoBehaviour
{
    [SerializeField] private ARSession session;
    [SerializeField] private ARSessionOrigin sessionOrigin;
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private TargetHandler targetHandler;
    [SerializeField] private GameObject qrCodeScanningPanel;
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private GameObject map;
    [SerializeField] private GameObject bottomBar;
    [SerializeField] private float requiredHoldTime;
    [SerializeField] private Image scanProgressCircle; // ✅ Radial progress circle
    [SerializeField] private GameObject statusPanel; // 
    [SerializeField] private GameObject progressBarContainer; 
    [SerializeField] private Slider progressBarSlider;     
    [SerializeField] private float stabilizationDuration = 1.5f;

    private bool isStabilizing = false;
    private float stabilizationTimer = 0f;


    private Dictionary<string, ARAnchor> qrAnchors = new Dictionary<string, ARAnchor>();

    private TourManager tourManager;
    private FloorTransitionManager floorTransitionManager;
    private AltitudeDetector altitudeDetector;

    private Texture2D cameraImageTexture;
    private IBarcodeReader reader = new BarcodeReader();
    public bool scanningEnabled = false;

    // ✅ Progress tracking
    private string lastDetectedQr = null;
    private float qrHoldTime = 0f;
    private float scanCooldown = 2.0f;
    private float cooldownTimer = 0f;

    private void Start()
    {
        tourManager = FindObjectOfType<TourManager>();
        floorTransitionManager = FindObjectOfType<FloorTransitionManager>();
        altitudeDetector = FindObjectOfType<AltitudeDetector>();
    }

    private void Update()
    {
        if (isStabilizing)
        {
            stabilizationTimer += Time.deltaTime;
            progressBarSlider.value = stabilizationTimer;

            if (stabilizationTimer >= stabilizationDuration)
            {
                progressBarContainer.SetActive(false);
                map.SetActive(true);
                bottomBar.SetActive(true);
                statusPanel.SetActive(true);
                isStabilizing = false;
            }
        }
    }


    private void OnEnable()
    {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    private void OnDisable()
    {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!scanningEnabled) return;

        // Cooldown after a successful scan
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            if (scanProgressCircle) scanProgressCircle.fillAmount = 0;
            return;
        }

        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) return;

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGB24,
            transformation = XRCpuImage.Transformation.None
        };

        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);
        image.Convert(conversionParams, buffer);
        image.Dispose();

        cameraImageTexture = new Texture2D(conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, conversionParams.outputFormat, false);
        cameraImageTexture.LoadRawTextureData(buffer);
        cameraImageTexture.Apply();
        buffer.Dispose();

        var result = reader.Decode(cameraImageTexture.GetPixels32(), cameraImageTexture.width, cameraImageTexture.height);

        if (result != null)
        {
            if (result.Text == lastDetectedQr)
            {
                qrHoldTime += Time.deltaTime;
                float progress = Mathf.Clamp01(qrHoldTime / requiredHoldTime);
                if (scanProgressCircle) scanProgressCircle.fillAmount = progress;

                if (qrHoldTime >= requiredHoldTime)
                {
                    Debug.Log($"[QrCodeRecenter] ✅ Confirmed scan: {result.Text}");

                    Handheld.Vibrate(); // 📳 Only vibration

                    ToggleScanning();
                    SetQrCodeRecenterTarget(result.Text);

                    // RecentertoQR(result.Text);

                    lastDetectedQr = null;
                    qrHoldTime = 0f;
                    cooldownTimer = scanCooldown;

                    if (scanProgressCircle) scanProgressCircle.fillAmount = 0;
                }
            }
            else
            {
                lastDetectedQr = result.Text;
                qrHoldTime = 0f;
                if (scanProgressCircle) scanProgressCircle.fillAmount = 0;
            }
        }
        else
        {
            lastDetectedQr = null;
            qrHoldTime = 0f;
            if (scanProgressCircle) scanProgressCircle.fillAmount = 0;
        }
    }

    private void SetQrCodeRecenterTarget(string targetText)
    {
        TargetFacade currentTarget = targetHandler.GetCurrentTargetByTargetText(targetText);
        Debug.Log($"[QrCodeRecenter] ✅ Transition to recenter map: {currentTarget?.Name}");

        if (currentTarget != null)
        {
            session.Reset();
            sessionOrigin.transform.position = currentTarget.transform.position;
            sessionOrigin.transform.rotation = currentTarget.transform.rotation;

            CreateAnchor(targetText, currentTarget.transform.position, currentTarget.transform.rotation);

            StartStabilizationProgressBar();

            if (floorTransitionManager != null)
            {
                floorTransitionManager.UpdateDetailsFromScanning(currentTarget.Floor, currentTarget.Building);
                if (floorTransitionManager.GetCurrentState() == FloorTransitionManager.FloorState.NavigatingNewFloor)
                {
                    floorTransitionManager.QRCodeScanned();
                }
            }

            if (tourManager != null &&
                targetText == tourManager.startingPoint.Name &&
                tourManager.GetCurrentState() == TourManager.TourState.WaitingForScan)
            {
                tourManager.OnQRCodeScannedAtStartingPoint();
            }

            if (altitudeDetector != null && altitudeDetector.altitudeHasChanged)
            {
                altitudeDetector.OnQRCodeScanned();
            }
        }
    }

    private void CreateAnchor(string qrCodeName, Vector3 position, Quaternion rotation)
    {
        if (anchorManager == null)
        {
            Debug.LogWarning("[QrCodeRecenter] ARAnchorManager not assigned!");
            return;
        }

        if (qrAnchors.TryGetValue(qrCodeName, out ARAnchor existingAnchor))
        {
            Destroy(existingAnchor.gameObject);
            qrAnchors.Remove(qrCodeName);
        }

        GameObject anchorObject = new GameObject($"QR_Anchor_{qrCodeName}");
        anchorObject.transform.position = position;
        anchorObject.transform.rotation = rotation;

        ARAnchor newAnchor = anchorObject.AddComponent<ARAnchor>();

        if (newAnchor != null)
        {
            qrAnchors[qrCodeName] = newAnchor;
            Debug.Log($"[QrCodeRecenter] ✅ Created anchor for QR code '{qrCodeName}' at {position}");
        }
        else
        {
            Debug.LogWarning($"[QrCodeRecenter] ❌ Failed to create anchor for '{qrCodeName}'!");
            Destroy(anchorObject);
        }
    }

    public void ToggleScanning()
    {
        Debug.Log($"[QrCodeRecenter] Toggle Scanning: {scanningEnabled}");
        scanningEnabled = !scanningEnabled;
        qrCodeScanningPanel.SetActive(scanningEnabled);
        map.SetActive(!scanningEnabled);
        bottomBar.SetActive(!scanningEnabled);
        statusPanel.SetActive(!scanningEnabled);

        // Reset UI
        if (scanProgressCircle) scanProgressCircle.fillAmount = 0;
        qrHoldTime = 0f;
        lastDetectedQr = null;
    }

    void StartStabilizationProgressBar()
    {
        stabilizationTimer = 0f;
        isStabilizing = true;

        progressBarSlider.minValue = 0;
        progressBarSlider.maxValue = stabilizationDuration;
        progressBarSlider.value = 0;

        progressBarContainer.SetActive(true);
        map.SetActive(false);
        bottomBar.SetActive(false);
        statusPanel.SetActive(false);
    }
}
