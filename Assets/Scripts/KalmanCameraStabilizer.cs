using UnityEngine;

public class KalmanCameraStabilizer : MonoBehaviour
{
    public Vector3 previousPosition;
    public Vector3 previousRotation;

    [Header("Position Kalman Filter Settings")]
    public float processNoisePos = 5e-4f;
    public float measurementNoisePos = 3e-2f;
    public float estimatedErrorPos = 5f;

    [Header("Rotation Kalman Filter Settings")]
    public float processNoiseRot = 5e-4f;
    public float measurementNoiseRot = 3e-2f;
    public float estimatedErrorRot = 5f;

    private Vector3 kalmanEstimatePos;
    private Vector3 kalmanErrorPos;
    private Vector3 kalmanGainPos;

    private Vector3 kalmanEstimateEuler;
    private Vector3 kalmanErrorEuler;
    private Vector3 kalmanGainEuler;

    private bool initialized = false;

    // NEW: Raw sensor data cache
    private Vector3 rawPosition;
    private Vector3 rawEuler;

    void Update()
    {
        // Cache raw data before filtering
        rawPosition = transform.position;
        rawEuler = transform.rotation.eulerAngles;

        Vector3 measuredPosition = rawPosition;
        Vector3 measuredEulerAngles = rawEuler;

        if (!initialized)
        {
            kalmanEstimatePos = measuredPosition;
            kalmanErrorPos = Vector3.one * estimatedErrorPos;

            kalmanEstimateEuler = measuredEulerAngles;
            kalmanErrorEuler = Vector3.one * estimatedErrorRot;

            initialized = true;
        }

        // --- POSITION FILTER ---
        kalmanErrorPos += Vector3.one * processNoisePos;

        kalmanGainPos = new Vector3(
            kalmanErrorPos.x / (kalmanErrorPos.x + measurementNoisePos),
            kalmanErrorPos.y / (kalmanErrorPos.y + measurementNoisePos),
            kalmanErrorPos.z / (kalmanErrorPos.z + measurementNoisePos)
        );

        kalmanEstimatePos = new Vector3(
            kalmanEstimatePos.x + kalmanGainPos.x * (measuredPosition.x - kalmanEstimatePos.x),
            kalmanEstimatePos.y + kalmanGainPos.y * (measuredPosition.y - kalmanEstimatePos.y),
            kalmanEstimatePos.z + kalmanGainPos.z * (measuredPosition.z - kalmanEstimatePos.z)
        );

        kalmanErrorPos = new Vector3(
            (1 - kalmanGainPos.x) * kalmanErrorPos.x,
            (1 - kalmanGainPos.y) * kalmanErrorPos.y,
            (1 - kalmanGainPos.z) * kalmanErrorPos.z
        );

        // --- ROTATION FILTER (Euler approximation) ---
        kalmanErrorEuler += Vector3.one * processNoiseRot;

        kalmanGainEuler = new Vector3(
            kalmanErrorEuler.x / (kalmanErrorEuler.x + measurementNoiseRot),
            kalmanErrorEuler.y / (kalmanErrorEuler.y + measurementNoiseRot),
            kalmanErrorEuler.z / (kalmanErrorEuler.z + measurementNoiseRot)
        );

        kalmanEstimateEuler = new Vector3(
            kalmanEstimateEuler.x + kalmanGainEuler.x * (measuredEulerAngles.x - kalmanEstimateEuler.x),
            kalmanEstimateEuler.y + kalmanGainEuler.y * (measuredEulerAngles.y - kalmanEstimateEuler.y),
            kalmanEstimateEuler.z + kalmanGainEuler.z * (measuredEulerAngles.z - kalmanEstimateEuler.z)
        );

        kalmanErrorEuler = new Vector3(
            (1 - kalmanGainEuler.x) * kalmanErrorEuler.x,
            (1 - kalmanGainEuler.y) * kalmanErrorEuler.y,
            (1 - kalmanGainEuler.z) * kalmanErrorEuler.z
        );

        // Apply filtered results
        previousPosition = rawPosition;
        previousRotation = rawEuler;
        transform.position = kalmanEstimatePos;
        transform.rotation = Quaternion.Euler(kalmanEstimateEuler);
    }

    public string GetDebugInfo()
    {
        return $"Filtered Pos: ({kalmanEstimatePos.x:F2}, {kalmanEstimatePos.y:F2}, {kalmanEstimatePos.z:F2})\n" +
               $"Filtered Rot: ({kalmanEstimateEuler.x:F2}, {kalmanEstimateEuler.y:F2}, {kalmanEstimateEuler.z:F2})";
    }

    // NEW: Public accessors for raw position and rotation
    public Vector3 GetRawPosition()
    {
        return rawPosition;
    }

    public Vector3 GetRawRotation()
    {
        return rawEuler;
    }
}
