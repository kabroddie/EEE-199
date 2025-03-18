using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;

public class IndicatorArrowController : MonoBehaviour
{
    [SerializeField] private GameObject indicatorArrow; // Assign the 3D arrow
    [SerializeField] private Camera arCamera; // Assign the AR Camera
    [SerializeField] private float rotationSpeed = 5.0f; // Adjust rotation smoothness

    [SerializeField] private Vector3 arrowForwardOffset = new Vector3(0, 0, 90); // ✅ Adjust model’s default orientation

    /// <summary>
    /// For smoothing the arrow’s rotation
    /// </summary>
    [SerializeField] private float positionSmoothness = 1.0f; // ✅ Adjust smoothing level
    private Vector3 stabilizedPosition; // ✅ Stores the smoothed position

    private void Start()
    {
        // If no camera is assigned, use the main camera
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        // ✅ Initialize the stabilized position
        stabilizedPosition = indicatorArrow.transform.position;
    }

    private void Update()
    {
        if (indicatorArrow != null && arCamera != null)

        {
            // ✅ Get the intended target position (based on camera movement)
            Vector3 targetPosition = arCamera.transform.position;

            // ✅ Apply smoothing to reduce jittering (Low-pass filter)
            stabilizedPosition = Vector3.Lerp(stabilizedPosition, targetPosition, positionSmoothness);

            // ✅ Set the indicator's stabilized position
            indicatorArrow.transform.position = stabilizedPosition;

            // ✅ Rotate the arrow based on the camera's Y-axis
            Quaternion cameraRotation = Quaternion.Euler(0, arCamera.transform.eulerAngles.y, 0);
            Quaternion targetRotation = cameraRotation * Quaternion.Euler(arrowForwardOffset);

            // ✅ Apply smooth rotation
            indicatorArrow.transform.rotation = Quaternion.Slerp(
                indicatorArrow.transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
        }

    }
}
