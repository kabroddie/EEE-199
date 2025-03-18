using UnityEngine;
using Unity.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Linq;
using ZXing;

public class QrCodeRecenter : MonoBehaviour
{
    [SerializeField]    
    private ARSession session;

    [SerializeField]
    private ARSessionOrigin sessionOrigin;

    [SerializeField]
    private ARCameraManager cameraManager;

    [SerializeField]
    private TargetHandler targetHandler;

    [SerializeField]
    private GameObject qrCodeScanningPanel;

    [SerializeField]
    private ARAnchorManager anchorManager; // ✅ Added ARAnchorManager
    [SerializeField]
    private GameObject map;

    private Dictionary<string, ARAnchor> qrAnchors = new Dictionary<string, ARAnchor>(); // ✅ Stores anchors for each QR

    private TourManager tourManager;

    private FloorTransitionManager floorTransitionManager; // ✅ Reference to FloorTransitionManager

    private Texture2D cameraImageTexture;
    private IBarcodeReader reader = new BarcodeReader();
    private bool scanningEnabled = false;

    private void Start()
    {
        // ✅ Find TourManager instance
        tourManager = FindObjectOfType<TourManager>();
        floorTransitionManager = FindObjectOfType<FloorTransitionManager>();
    }

    private void OnEnable() {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    private void OnDisable() {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs) {

        if (!scanningEnabled)
        {
            return;
        }
        
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            return;
        }

        // ToggleScanning(false);
        
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

        cameraImageTexture = new Texture2D(
            conversionParams.outputDimensions.x,
            conversionParams.outputDimensions.y,
            conversionParams.outputFormat,
            false
        );

        cameraImageTexture.LoadRawTextureData(buffer);

        cameraImageTexture.Apply();

        buffer.Dispose();

        var result = reader.Decode(cameraImageTexture.GetPixels32(), cameraImageTexture.width, cameraImageTexture.height);

        if (result != null)
        {
            ToggleScanning();
            SetQrCodeRecenterTarget(result.Text);
            // ToggleScanning(false);
            
        }
            
    }

    private void SetQrCodeRecenterTarget(string targetText)
    {
        TargetFacade currentTarget = targetHandler.GetCurrentTargetByTargetText(targetText);
        Debug.Log($"[QrCodeRecenter] ✅ Transition to recenter map: {currentTarget.Name}");
        if (currentTarget != null)
        {
            session.Reset();

            sessionOrigin.transform.position = currentTarget.transform.position;
            sessionOrigin.transform.rotation = currentTarget.transform.rotation;

            // CreateOrUpdateAnchor(targetText, currentTarget.transform.position, currentTarget.transform.rotation);

            CreateAnchor(targetText, currentTarget.transform.position, currentTarget.transform.rotation);

            if (floorTransitionManager != null)
            {
                floorTransitionManager.UpdateCurrentFloorFromScanning(currentTarget.Floor);
                if (floorTransitionManager.GetCurrentState() == FloorTransitionManager.FloorState.NavigatingNewFloor)
                {
                   
                    floorTransitionManager.QRCodeScanned();
                }
            }

            TourManager.TourState tourstate = tourManager.GetCurrentState();

             // ✅ Check if this is the tour's starting point
            if (tourManager != null && targetText == tourManager.startingPoint.Name && tourstate == TourManager.TourState.WaitingForScan) // Ensure it matches the defined starting point
            {
                tourManager.OnQRCodeScannedAtStartingPoint();
            }
        }
    }

    // private void CreateAnchor(string qrCodeName, Vector3 position, Quaternion rotation)
    // {
    //     if (anchorManager == null)
    //     {
    //         Debug.LogWarning("[QrCodeRecenter] ARAnchorManager not assigned!");
    //         return;
    //     }

    //     // ✅ Always replace the previous anchor
    //     if (qrAnchors.TryGetValue(qrCodeName, out ARAnchor existingAnchor))
    //     {
    //         Destroy(existingAnchor.gameObject);
    //         qrAnchors.Remove(qrCodeName);
    //     }

    //     GameObject anchorObject = new GameObject($"QR_Anchor_{qrCodeName}");
    //     anchorObject.transform.position = position;
    //     anchorObject.transform.rotation = rotation;

    //     Pose anchorPose = new Pose(position, rotation);
    //     ARAnchor newAnchor = anchorManager.AddAnchor(anchorPose);

    //     if (newAnchor != null)
    //     {
    //         qrAnchors[qrCodeName] = newAnchor; // ✅ Store new anchor
    //         Debug.Log($"[QrCodeRecenter] ✅ Created anchor for QR code '{qrCodeName}' at {position}");
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"[QrCodeRecenter] ❌ Failed to create anchor for '{qrCodeName}'!");
    //         Destroy(anchorObject); // Cleanup if anchor creation fails
    //     }
    // }

    private void CreateAnchor(string qrCodeName, Vector3 position, Quaternion rotation)
    {
        if (anchorManager == null)
        {
            Debug.LogWarning("[QrCodeRecenter] ARAnchorManager not assigned!");
            return;
        }

        // ✅ Always replace the previous anchor
        if (qrAnchors.TryGetValue(qrCodeName, out ARAnchor existingAnchor))
        {
            Destroy(existingAnchor.gameObject);
            qrAnchors.Remove(qrCodeName);
        }

        // ✅ Create an empty GameObject at the position
        GameObject anchorObject = new GameObject($"QR_Anchor_{qrCodeName}");
        anchorObject.transform.position = position;
        anchorObject.transform.rotation = rotation;

        // ✅ Add ARAnchor Component (Fixes Obsolete Method)
        ARAnchor newAnchor = anchorObject.AddComponent<ARAnchor>();

        if (newAnchor != null)
        {
            qrAnchors[qrCodeName] = newAnchor; // ✅ Store new anchor
            Debug.Log($"[QrCodeRecenter] ✅ Created anchor for QR code '{qrCodeName}' at {position}");
        }
        else
        {
            Debug.LogWarning($"[QrCodeRecenter] ❌ Failed to create anchor for '{qrCodeName}'!");
            Destroy(anchorObject); // Cleanup if anchor creation fails
        }
    
    }
    public void ToggleScanning()
    {
        
        scanningEnabled = !scanningEnabled;
        qrCodeScanningPanel.SetActive(scanningEnabled);
        map.SetActive(!scanningEnabled);
    }

    // ✅ Default version (for UI Button)
    // public void ToggleScanning()
    // {
    //     ToggleScanning(!scanningEnabled); // ✅ Calls the explicit version
    // }

    // // ✅ Explicit version (for script control)
    // public void ToggleScanning(bool enable)
    // {
    //     scanningEnabled = enable;
    //     qrCodeScanningPanel.SetActive(enable);
    // }

}
