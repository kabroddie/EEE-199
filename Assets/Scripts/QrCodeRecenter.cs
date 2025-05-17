// using UnityEngine;
// using UnityEngine.UI;
// using Unity.Collections;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;
// using System.Collections.Generic;
// using ZXing;

// public class QrCodeRecenter : MonoBehaviour
// {
//     [Header("AR & UI References")]
//     [SerializeField] private ARSessionOrigin sessionOrigin;
//     [SerializeField] private ARCameraManager cameraManager;
//     [SerializeField] private TargetHandler targetHandler;
//     [SerializeField] private GameObject qrCodeScanningPanel;
//     [SerializeField] private ARAnchorManager anchorManager;
//     [SerializeField] private GameObject map;
//     [SerializeField] private GameObject bottomBar;
//     [SerializeField] private Image scanProgressCircle;

//     // Store one anchor per QR code key
//     private Dictionary<string, ARAnchor> qrAnchors = new Dictionary<string, ARAnchor>();

//     private TourManager tourManager;
//     private FloorTransitionManager floorTransitionManager;
//     private AltitudeDetector altitudeDetector;
//     private IBarcodeReader reader = new BarcodeReader();

//     private bool scanningEnabled = false;
//     private string lastDetectedQr = null;
//     private float qrHoldTime = 0f;
//     private const float requiredHoldTime = 1.5f;
//     private const float scanCooldown = 2.0f;
//     private float cooldownTimer = 0f;

//     void Start()
//     {
//         tourManager = FindObjectOfType<TourManager>();
//         floorTransitionManager = FindObjectOfType<FloorTransitionManager>();
//         altitudeDetector = FindObjectOfType<AltitudeDetector>();
//         qrCodeScanningPanel.SetActive(false);
//     }

//     void OnEnable()
//     {
//         cameraManager.frameReceived += OnCameraFrameReceived;
//     }

//     void OnDisable()
//     {
//         cameraManager.frameReceived -= OnCameraFrameReceived;
//     }

//     /// <summary>
//     /// Toggle QR scanning UI on/off.
//     /// </summary>
//     public void ToggleScanning()
//     {
//         scanningEnabled = !scanningEnabled;
//         qrCodeScanningPanel.SetActive(scanningEnabled);
//         map.SetActive(!scanningEnabled);
//         bottomBar.SetActive(!scanningEnabled);

//         // Reset progress and timers
//         scanProgressCircle.fillAmount = 0f;
//         qrHoldTime = 0f;
//         lastDetectedQr = null;
//         cooldownTimer = 0f;
//     }

//     private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
//     {
//         if (!scanningEnabled)
//             return;

//         // Enforce a cooldown between scans
//         if (cooldownTimer > 0f)
//         {
//             cooldownTimer -= Time.deltaTime;
//             return;
//         }

//         // Acquire latest CPU image
//         if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
//             return;

//         // Convert to Texture2D
//         var conv = new XRCpuImage.ConversionParams
//         {
//             inputRect = new RectInt(0, 0, image.width, image.height),
//             outputDimensions = new Vector2Int(image.width, image.height),
//             outputFormat = TextureFormat.RGB24,
//             transformation = XRCpuImage.Transformation.None
//         };

//         int size = image.GetConvertedDataSize(conv);
//         var buffer = new NativeArray<byte>(size, Allocator.Temp);
//         image.Convert(conv, buffer);
//         image.Dispose();

//         var tex = new Texture2D(conv.outputDimensions.x, conv.outputDimensions.y, conv.outputFormat, false);
//         tex.LoadRawTextureData(buffer);
//         tex.Apply();
//         buffer.Dispose();

//         // Decode with ZXing
//         var result = reader.Decode(tex.GetPixels32(), tex.width, tex.height);
//         Destroy(tex);

//         if (result != null)
//         {
//             if (result.Text == lastDetectedQr)
//             {
//                 qrHoldTime += Time.deltaTime;
//                 scanProgressCircle.fillAmount = Mathf.Clamp01(qrHoldTime / requiredHoldTime);

//                 if (qrHoldTime >= requiredHoldTime)
//                 {
//                     Handheld.Vibrate();
//                     ToggleScanning();
//                     RecenterToQR(result.Text);

//                     cooldownTimer = scanCooldown;
//                     qrHoldTime = 0f;
//                     lastDetectedQr = null;
//                 }
//             }
//             else
//             {
//                 lastDetectedQr = result.Text;
//                 qrHoldTime = 0f;
//                 scanProgressCircle.fillAmount = 0f;
//             }
//         }
//         else
//         {
//             lastDetectedQr = null;
//             qrHoldTime = 0f;
//             scanProgressCircle.fillAmount = 0f;
//         }
//     }

//     /// <summary>
//     /// Recenter the ARSessionOrigin so the AR camera moves to the QR's pose,
//     /// plus create an anchor at that spot for later stability.
//     /// </summary>
//     private void RecenterToQR(string qrKey)
//     {
//         TargetFacade target = targetHandler.GetCurrentTargetByTargetText(qrKey);
//         if (target == null)
//             return;

//         Vector3 pos = target.transform.position;
//         Quaternion rot = target.transform.rotation;

//         sessionOrigin.transform.position = pos;
//         sessionOrigin.transform.rotation = rot;

//         // Create or replace the anchor for this QR code
//         ARAnchor anchor = CreateAnchor(qrKey, pos, rot);

//         // Move the ARSessionOrigin so the camera ends up at 'pos/rot'
//         // sessionOrigin.transform.SetPositionAndRotation(
//         //     sessionOrigin.transform.position + (pos - sessionOrigin.camera.transform.position),
//         //     rot * Quaternion.Inverse(sessionOrigin.camera.transform.rotation) * sessionOrigin.transform.rotation
//         // );

        

//         // Floor transition
//         if (floorTransitionManager != null)
//         {
//             floorTransitionManager.UpdateDetailsFromScanning(target.Floor, target.Building);
//             if (floorTransitionManager.GetCurrentState() == FloorTransitionManager.FloorState.NavigatingNewFloor)
//                 floorTransitionManager.QRCodeScanned();
//         }

//         // Tour start check
//         if (tourManager != null &&
//             tourManager.startingPoint != null &&
//             qrKey == tourManager.startingPoint.Name &&
//             tourManager.GetCurrentState() == TourManager.TourState.WaitingForScan)
//         {
//             tourManager.OnQRCodeScannedAtStartingPoint();
//         }

//         // Reset altitude detector
//         if (altitudeDetector != null && altitudeDetector.altitudeHasChanged)
//         {
//             altitudeDetector.OnQRCodeScanned();
//         }
//     }

//     /// <summary>
//     /// Creates or replaces an ARAnchor for the given QR key at the specified pose.
//     /// Ensures one anchor per QR, replacing old ones on re-scan.
//     /// </summary>
//     private ARAnchor CreateAnchor(string qrKey, Vector3 position, Quaternion rotation)
//     {
//         // Remove previous
//         if (qrAnchors.TryGetValue(qrKey, out ARAnchor old) && old != null)
//         {
//             Destroy(old.gameObject);
//             qrAnchors.Remove(qrKey);
//         }

//         // Create new GameObject and ARAnchor component
//         var go = new GameObject($"QR_Anchor_{qrKey}");
//         go.transform.position = position;
//         go.transform.rotation = rotation;
//         var newAnchor = go.AddComponent<ARAnchor>();

//         if (newAnchor != null)
//             qrAnchors[qrKey] = newAnchor;
//         else
//             Destroy(go);

//         return newAnchor;
//     }
// }




using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using ZXing;

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
}