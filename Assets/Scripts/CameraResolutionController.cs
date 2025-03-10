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
        
        // Check if ARCameraManager is found
        if (arCameraManager == null)
        {
            Debug.LogError("ARCameraManager component not found on this GameObject.");
            return;
        }
        
        // Check if subsystem is available
        if (arCameraManager.subsystem == null)
        {
            Debug.LogError("No AR Camera subsystem found. Are you running on a device that supports AR?");
            return;
        }

        // Now try setting resolution
        SetLowestResolution();
    }

    void SetLowestResolution()
    {
        // If descriptor is null, we can’t proceed
        if (arCameraManager.descriptor == null)
        {
            Debug.LogError("ARCameraManager descriptor is null. The AR subsystem may not be initialized yet.");
            return;
        }

        // Check if camera configs are supported
        if (!arCameraManager.descriptor.supportsCameraConfigurations)
        {
            Debug.LogWarning("AR Camera configurations are not supported on this device or AR subsystem.");
            return;
        }

        // Get and set configurations
        using (NativeArray<XRCameraConfiguration> configurations = arCameraManager.GetConfigurations(Allocator.Temp))
        {
            if (configurations.Length == 0)
            {
                Debug.LogWarning("No camera configurations found.");
                return;
            }

            XRCameraConfiguration lowestResolution = configurations[0];

            foreach (var config in configurations)
            {
                // Compare by resolution.x (width)
                if (config.resolution.x < lowestResolution.resolution.x)
                {
                    lowestResolution = config;
                }
            }

            arCameraManager.currentConfiguration = lowestResolution;
            Debug.Log($"Set AR Camera Resolution to: {lowestResolution.resolution.x}x{lowestResolution.resolution.y}");
        }
    }
}
