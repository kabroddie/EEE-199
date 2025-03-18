using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;

public class CameraResolutionController : MonoBehaviour
{
    private ARCameraManager arCameraManager;

    void Start()
    {
        arCameraManager = GetComponent<ARCameraManager>();

        if (arCameraManager == null)
        {
            Debug.LogError("[CameraResolutionController] ❌ ARCameraManager component not found on this GameObject.");
            return;
        }

        if (arCameraManager.subsystem == null)
        {
            Debug.LogError("[CameraResolutionController] ❌ No AR Camera subsystem found. Are you running on a device that supports AR?");
            return;
        }

        // ✅ Set to lowest resolution
        SetLowestResolution();
    }

    void SetLowestResolution()
    {
        if (arCameraManager.descriptor == null)
        {
            Debug.LogError("[CameraResolutionController] ❌ ARCameraManager descriptor is null. The AR subsystem may not be initialized yet.");
            return;
        }

        if (!arCameraManager.descriptor.supportsCameraConfigurations)
        {
            Debug.LogWarning("[CameraResolutionController] ⚠️ AR Camera configurations are not supported on this device or AR subsystem.");
            return;
        }

        using (NativeArray<XRCameraConfiguration> configurations = arCameraManager.GetConfigurations(Allocator.Temp))
        {
            if (configurations.Length == 0)
            {
                Debug.LogWarning("[CameraResolutionController] ⚠️ No camera configurations found.");
                return;
            }

            // ✅ Find the lowest resolution
            XRCameraConfiguration lowestResolution = configurations[0];
            foreach (var config in configurations)
            {
                if (config.resolution.x < lowestResolution.resolution.x)
                {
                    lowestResolution = config;
                }
            }

            // ✅ Apply lowest resolution
            arCameraManager.currentConfiguration = lowestResolution;
            Debug.Log($"[CameraResolutionController] ✅ Set Camera Resolution to: {lowestResolution.resolution.x}x{lowestResolution.resolution.y}");
        }
    }
}
