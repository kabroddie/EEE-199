using UnityEngine;
using Unity.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
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

    private TourManager tourManager;

    [SerializeField]
    private GameObject readyForTourButton;

    private FloorTransitionManager floorTransitionManager; // ✅ Reference to FloorTransitionManager

    private Texture2D cameraImageTexture;
    private IBarcodeReader reader = new BarcodeReader();
    private bool scanningEnabled = false;

    private void Start()
    {
        // ✅ Find TourManager instance
        tourManager = FindObjectOfType<TourManager>();
        floorTransitionManager = FindObjectOfType<FloorTransitionManager>();

        // ✅ Ensure the "Ready for Tour" button is hidden at the start
        if (readyForTourButton != null)
        {
            readyForTourButton.SetActive(false);
        }
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
            SetQrCodeRecenterTarget(result.Text);
            ToggleScanning();
        }
            
    }

    private void SetQrCodeRecenterTarget(string targetText)
    {
        TargetFacade currentTarget = targetHandler.GetCurrentTargetByTargetText(targetText);
        if (currentTarget != null)
        {
            session.Reset();

            sessionOrigin.transform.position = currentTarget.transform.position;
            sessionOrigin.transform.rotation = currentTarget.transform.rotation;

            if (floorTransitionManager != null)
            {
                floorTransitionManager.UpdateCurrentFloorFromScanning(currentTarget.Floor);
            }

             // ✅ Check if this is the tour's starting point
            if (tourManager != null && targetText == "Entry") // Ensure it matches the defined starting point
            {
                tourManager.OnQRCodeScannedAtStartingPoint();
                ShowReadyForTourButton();
            }
        }
    }

    private void ShowReadyForTourButton()
    {
        if (readyForTourButton != null)
        {
            readyForTourButton.SetActive(true); // ✅ Show the "Ready for Tour" button
        }
    }

    public void ToggleScanning()
    {
        scanningEnabled = !scanningEnabled;
        qrCodeScanningPanel.SetActive(scanningEnabled);
    }
}
